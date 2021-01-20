﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotLiquid;
using Rock.Common;

namespace Rock.Lava.DotLiquid
{
    /// <summary>
    /// Initialization class for the DotLiquid Templating Engine.
    /// </summary>
    public partial class DotLiquidEngine : LavaEngineBase
    {
        public override string EngineName
        {
            get
            {
                return "DotLiquid";
            }
        }

        public override LavaEngineTypeSpecifier EngineType
        {
            get
            {
                return LavaEngineTypeSpecifier.DotLiquid;
            }
        }

        public override ILavaContext NewContext( IDictionary<string, object> mergeFields = null )
        {
            var dotLiquidContext = new global::DotLiquid.Context();

            // Set the transformation function for converting a Lava Context to a DotLiquid Context.
            // This is needed to allow the Lava context parameter in a filter function to be identified and 
            // injected in a way that is framework-agnostic.
            dotLiquidContext.FilterContextParameterType = Template.FilterContextParameterType;
            dotLiquidContext.FilterContextParameterTransformer = Template.FilterContextParameterTransformer;

            var context = new DotLiquidLavaContext( dotLiquidContext );

            context.SetMergeFieldValues( mergeFields );

            return context;
        }

        /// <summary>
        /// Initializes Rock's Lava system (which uses DotLiquid)
        /// Doing this in startup will force the static Liquid class to get instantiated
        /// so that the standard filters are loaded prior to the custom RockFilter.
        /// This is to allow the custom 'Date' filter to replace the standard Date filter.
        /// </summary>
        public override void OnSetConfiguration( LavaEngineConfigurationOptions options )
        {
            // DotLiquid uses a RubyDateFormat by default,
            // but since we aren't using Ruby, we want to disable that
            Liquid.UseRubyDateFormat = false;

            /* 2020-05-20 MDP (actually this comment was here a long time ago)
                NOTE: This means that all the built in template filters,
                and the RockFilters, will use CSharpNamingConvention.

                For example the dotliquid documentation says to do this for formatting dates: 
                {{ some_date_value | date:"MMM dd, yyyy" }}

                However, if CSharpNamingConvention is enabled, it needs to be: 
                {{ some_date_value | Date:"MMM dd, yyyy" }}
            */

            Template.NamingConvention = new global::DotLiquid.NamingConventions.CSharpNamingConvention();

            if ( options.FileSystem == null )
            {
                options.FileSystem = new DotLiquidFileSystem( new LavaNullFileSystem() );
            }

            Template.FileSystem = new DotLiquidFileSystem( options.FileSystem );

            Template.RegisterSafeType( typeof( Enum ), o => o.ToString() );
            Template.RegisterSafeType( typeof( DBNull ), o => null );

            RegisterFilters( options.FilterImplementationTypes );

            // Register all Types that implement ILavaDataObject as safe to render.
            RegisterSafeType( typeof( Rock.Lava.ILavaDataDictionary ) );
        }

        private void RegisterFilters( IEnumerable<Type> filterImplementationTypes )
        {
            Template.FilterContextParameterType = typeof( ILavaContext );

            Template.FilterContextParameterTransformer = ( context ) =>
            {
                // Wrap the DotLiquid context in a framework-agnostic Lava context.
                return new DotLiquidLavaContext( context );
            };

            // Register custom filters last, so they can override built-in filters of the same name.
            if ( filterImplementationTypes != null )
            {
                foreach ( var filterImplementationType in filterImplementationTypes )
                {
                    var methodsGroupedByName = filterImplementationType.GetMethods( BindingFlags.Public | BindingFlags.Static ).AsQueryable().GroupBy( k => k.Name, v => v );

                    foreach (var methodGroup in methodsGroupedByName )
                    {
                        var filterName = methodGroup.Key;
                        var filterMethodInfos = methodGroup.OrderBy( m => m.GetParameters().Length ).ToList();

                        // Define the DotLiquid-compatible function that will wrap the Lava filter.
                        // When the wrapper function is executed by DotLiquid, it performs some necessary pre-processing before executing the Lava filter.
                        Func<Context, List<object>, object> filterFunctionWrapper = ( Context context, List<object> args ) =>
                        {
                            // Get the filter method that best matches the provided argument list.
                            MethodInfo filterMethodInfo = null;

                            if ( filterMethodInfos.Count == 1 )

                            {
                                filterMethodInfo = filterMethodInfos.First();
                            }
                            else
                            {
                                // Find the method that best matches the provided list of arguments.                                
                                filterMethodInfo = GetMatchedFilterFunction( filterMethodInfos, args.Count );
                            }

                            var parameterInfos = filterMethodInfo.GetParameters();

                            GetLavaFilterCompatibleArguments( filterName, args, parameterInfos, context );

                            // Execute the static filter function and return the result.
                            var result = filterMethodInfo.Invoke( null, args.ToArray() );

                            return result;
                        };

                        // Register the set of filters for each method name.                
                        Strainer.RegisterFilter( methodGroup.Key, filterFunctionWrapper );
                    }
                }
            }
        }

        /// <summary>
        /// Translate a set of DotLiquid filter arguments to a set of arguments that are compatible with a Lava filter.
        /// </summary>
        private void GetLavaFilterCompatibleArguments( string filterName, List<object> args, ParameterInfo[] lavaFilterFunctionParams, Context dotLiquidContext )
        {
            // Add the DotLiquid Context wrapped in a LavaContext.
            if ( lavaFilterFunctionParams.Length > 0 && lavaFilterFunctionParams[0].ParameterType == typeof( ILavaContext ) )
            {
                args.Insert( 0, new DotLiquidLavaContext( dotLiquidContext ) );
            }

            // Unwrap DotLiquidLavaDataObjectProxy objects
            for ( int i = 0; i < args.Count; i ++ )
            {
                if ( ( args[i] is ILavaDataDictionarySource ) ) //.GetType() == typeof( DotLiquidLavaDataObjectProxy ) )
                {
                    args[i] = ( (ILavaDataDictionarySource)args[i] ).GetLavaDataDictionary();
                }

                if ( args[i] is DropProxy )
                {
                    args[i] = ( (DropProxy)args[i] ).ConvertToValueType();
                }

            }
            // Add in any missing parameters with the default values defined for the filter method.
            if ( lavaFilterFunctionParams.Length > args.Count )
            {
                for ( int i = args.Count; i < lavaFilterFunctionParams.Length; ++i )
                {
                    if ( ( lavaFilterFunctionParams[i].Attributes & ParameterAttributes.HasDefault ) != ParameterAttributes.HasDefault )
                    {
                        throw new LavaException( "Error - Filter '{0}' does not have a default value for '{1}' and no value was supplied", filterName, lavaFilterFunctionParams[i].Name );
                    }
                    args.Add( lavaFilterFunctionParams[i].DefaultValue );
                }
            }
        }

        /// <summary>
        /// Get the filter method that best matches the supplied argument list.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="suppliedArgsCount"></param>
        /// <returns></returns>
        private MethodInfo GetMatchedFilterFunction( List<MethodInfo> candidateMethodInfoList, int suppliedArgsCount )
        {
            // Subtract the mandatory input object from the count of supplied filter arguments.
            if ( suppliedArgsCount > 0 )
            {
                suppliedArgsCount--;
            }

            foreach ( var methodInfo in candidateMethodInfoList )
            {
                bool isMatch = true;

                var parameterInfos = methodInfo.GetParameters();

                // Remove the input object that the filter is being applied to and the optional Lava context parameter from the count of required user-supplied arguments.
                var functionArgumentOffset = 1;

                var filterArgumentCount = parameterInfos.Length - 1;

                // If the filter accepts a context, it must be the first argument.
                if ( filterArgumentCount > 0 && parameterInfos[0].ParameterType == typeof( ILavaContext ) )
                {
                    filterArgumentCount--;
                    functionArgumentOffset++;
                }

                //
                if ( filterArgumentCount < suppliedArgsCount )
                {
                    continue;
                }
                else if ( filterArgumentCount == suppliedArgsCount )
                {
                    return methodInfo;
                }
                else
                {
                    // Number of filter arguments exceeds supplied arguments, so check if the additional parameters have default values.
                    for ( int i = suppliedArgsCount + functionArgumentOffset - 1; i < filterArgumentCount + functionArgumentOffset; i++ )
                    {
                        if ( ( parameterInfos[i].Attributes & ParameterAttributes.HasDefault ) != ParameterAttributes.HasDefault )
                        {
                            // No match
                            isMatch = false;
                            break;
                        }
                    }

                    if ( isMatch )
                    {
                        return methodInfo;
                    }
                }

            }

            // If an exact match could not be found, take the function with the most parameters and try to fill in the remainder with default values.
            return candidateMethodInfoList.LastOrDefault();
        }

        public override void RegisterSafeType( Type type, string[] allowedMembers = null )
        {
            if ( typeof( Rock.Lava.ILavaDataDictionarySource ).IsAssignableFrom( type ) )
            {
                Template.RegisterSafeType( type,
                    ( x ) =>
                    {
                        return ( (Rock.Lava.ILavaDataDictionarySource)x ).GetLavaDataDictionary();
                    } );
            }
            else if ( typeof( Rock.Lava.ILavaDataDictionary ).IsAssignableFrom( type ) )
            {
                Template.RegisterSafeType( typeof( Rock.Lava.ILavaDataDictionary ),
                    ( x ) =>
                    {
                        //var dataObject = new LavaDataObject( x );
                        return new DotLiquidLavaDataObjectProxy( x as ILavaDataDictionary );
                        //return dataObject;
                    } );
            }
            else
            {
                // Wrap the object in a DotLiquid compatible proxy that supports the IDictionary<string, object> interface.
                Template.RegisterSafeType( type,
                    ( x ) =>
                    {
                        var dataObject = new LavaDataObject( x );
                        return dataObject;
                    } );
            }
        }

        public override void RegisterTag( string name, Func<string, IRockLavaTag> factoryMethod )
        {
            if ( name == null )
            {
                throw new ArgumentException( "Name must be specified." );
            }

            name = name.Trim().ToLower();

            base.RegisterTag( name, factoryMethod );

            DotLiquidTagProxy.RegisterFactory( name, factoryMethod );

            // Register the proxy for the specified tag name.
            Template.RegisterTag<DotLiquidTagProxy>( name );
        }

        public override void RegisterBlock( string name, Func<string, IRockLavaBlock> factoryMethod )
        {
            if ( name == null )
            {
                throw new ArgumentException( "Name must be specified." );
            }

            name = name.Trim().ToLower();

            base.RegisterBlock( name, factoryMethod );

            DotLiquidBlockProxy.RegisterFactory( name, factoryMethod );

            // DotLiquid regards a Block as a special type of Tag.
            Template.RegisterTag<DotLiquidBlockProxy>( name );
        }

        /// <summary>
        /// Convert a LavaContext to a DotLiquid RenderParameters object. 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private RenderParameters GetDotLiquidRenderParametersFromLavaContext( ILavaContext context )
        {
            var renderSettings = new RenderParameters();

            if ( context == null )
            {
                context = NewContext();
            }

            var templateContext = context as DotLiquidLavaContext;

            if ( templateContext == null )
            {
                throw new LavaException( "Invalid LavaContext parameter. This context type is not compatible with the DotLiquid templating engine." );
            }

            var dotLiquidContext = templateContext.DotLiquidContext;

            // Set the transformation function for converting a DotLiquid Context to a Lava Context.
            // This tranformation allows the context to be injected into a filter in a way that is independent
            // of the underlying rendering framework.
            dotLiquidContext.FilterContextParameterType = Template.FilterContextParameterType;
            dotLiquidContext.FilterContextParameterTransformer = Template.FilterContextParameterTransformer;

            renderSettings.Context = dotLiquidContext;

            // Store the EnabledCommands setting in the DotLiquid Registers.
            //renderSettings.Context.Registers["EnabledCommands"] = string.Join( ",", context.GetEnabledCommands() );

            return renderSettings;
        }

        protected override bool OnTryRender( ILavaTemplate template, LavaRenderParameters parameters, out string output )
        {
        //    try
        //    {
        //        //var template = CreateNewDotLiquidTemplate( inputTemplate );
        //        var liquidTemplate = template as DotLiquidTemplateProxy;

        //        //liquidTemplate.
        //        //var renderSettings = GetDotLiquidRenderParametersFromLavaContext( context );
        //        IList<Exception> errors;

        //        var success = template.TryRender( context, out output, out errors ); // renderSettings );

        //        if ( !success )
        //        {
        //            // Process the first error.
        //            ProcessException( new LavaException("Rendering error", errors[0] ) );
        //        }

        //        return success;
        //    }
        //    catch ( Exception ex )
        //    {
        //        ProcessException( ex, out output );

        //        return false;
        //    }
        //}

        //protected override bool OnTryRender( string inputTemplate, out string output, ILavaContext context )
        //{
            try
            {
                var renderSettings = new RenderParameters();

                var templateContext = parameters.LavaContext as DotLiquidLavaContext;

                if ( templateContext == null )
                {
                    throw new LavaException( "Invalid LavaContext parameter. This context type is not compatible with the DotLiquid templating engine." );
                }

                var dotLiquidContext = templateContext.DotLiquidContext;

                // Set the transformation function for converting a DotLiquid Context to a Lava Context.
                // This tranformation allows the context to be injected into a filter in a way that is independent
                // of the underlying rendering framework.
                dotLiquidContext.FilterContextParameterType = Template.FilterContextParameterType;
                dotLiquidContext.FilterContextParameterTransformer = Template.FilterContextParameterTransformer;

                renderSettings.Context = dotLiquidContext;

                if ( parameters.ShouldEncodeStringsAsXml )
                {
                    renderSettings.ValueTypeTransformers = new Dictionary<Type, Func<object, object>>();
                    renderSettings.ValueTypeTransformers.Add( typeof( string ), EncodeStringTransformer );
                }

                // Call the Render method of the underlying DotLiquid template.
                var templateProxy = template as DotLiquidTemplateProxy;

                output = templateProxy.DotLiquidTemplate.Render( renderSettings );

                return true;
            }
            catch ( Exception ex )
            {
                ProcessException( ex, out output );

                return false;
            }
        }

        /// <summary>
        /// Encodes string values that are processed by a lava filter
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns></returns>
        private static object EncodeStringTransformer( object s )
        {
            string val = ( s as string );

            if ( !string.IsNullOrEmpty( val ) )
            {
                return val.EncodeXml();
            }
            else
            {
                return s;
            }
        }

        protected override ILavaTemplate OnParseTemplate( string inputTemplate )
        {
            // Create a new DotLiquid template and wrap it in a proxy for use with the Lava engine.
            var dotLiquidTemplate = CreateNewDotLiquidTemplate( inputTemplate );

            var lavaTemplate = new DotLiquidTemplateProxy( dotLiquidTemplate );

            return lavaTemplate;
        }

        public override bool AreEqualValue( object left, object right )
        {
            var condition = global::DotLiquid.Condition.Operators["=="];

            return condition( left, right );
        }

        private Template CreateNewDotLiquidTemplate( string inputTemplate )
        {
            var liquidTemplate = ConvertToLiquid( inputTemplate );

            var template = Template.Parse( liquidTemplate );

            /* 
             * 2/19/2020 - JPH
             * The DotLiquid library's Template object was not originally designed to be thread safe, but a PR has since
             * been merged into that repository to add this functionality (https://github.com/dotliquid/dotliquid/pull/220).
             * We have cherry-picked the PR's changes into our DotLiquid project, allowing the Template to operate safely
             * in a multithreaded context, which can happen often with our cached Template instances.
             *
             * Reason: Rock Issue #4084, Weird Behavior with Lava Includes
             */
            template.MakeThreadSafe();

            return template;
        }
    }
}
