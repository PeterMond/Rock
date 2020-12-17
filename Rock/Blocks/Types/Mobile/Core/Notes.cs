// <copyright>
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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Common.Mobile.Blocks.Content;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace Rock.Blocks.Types.Mobile.Core
{
    /// <summary>
    /// Displays entity notes to the user and allows adding new notes.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockMobileBlockType" />

    [DisplayName( "Notes" )]
    [Category( "Mobile > Core" )]
    [Description( "Displays entity notes to the user and allows adding new notes." )]
    [IconCssClass( "fa fa-sticky-note" )]

    #region Block Attributes

    [EntityTypeField( "Entity Type",
        Description = "The type of entity",
        IsRequired = false,
        Key = AttributeKey.ContextEntityType,
        Order = 0 )]

    [NoteTypeField( "Note Types",
        Description = "Optional list of note types to limit display to",
        AllowMultiple = true,
        IsRequired = false,
        Order = 1,
        Key = AttributeKey.NoteTypes )]

    #endregion

    [ContextAware]
    public class Notes : RockMobileBlockType
    {
        /// <summary>
        /// The block setting attribute keys for the MobileContent block.
        /// </summary>
        public static class AttributeKey
        {
            /// <summary>
            /// The context entity type key
            /// </summary>
            public const string ContextEntityType = "ContextEntityType";

            /// <summary>
            /// The note types key
            /// </summary>
            public const string NoteTypes = "NoteTypes";
        }

        /// <summary>
        /// Gets the type of the context entity.
        /// </summary>
        /// <value>
        /// The type of the context entity.
        /// </value>
        protected string ContextEntityType => GetAttributeValue( AttributeKey.ContextEntityType );

        /// <summary>
        /// Gets the note type unique identifiers selected in the block configuration.
        /// </summary>
        /// <value>
        /// The note type unique identifiers selected in the block configuration.
        /// </value>
        protected ICollection<Guid> NoteTypes => GetAttributeValue( AttributeKey.NoteTypes ).SplitDelimitedValues().AsGuidList();

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the required mobile application binary interface version required to render this block.
        /// </summary>
        /// <value>
        /// The required mobile application binary interface version required to render this block.
        /// </value>
        public override int RequiredMobileAbiVersion => 2;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        public override string MobileBlockType => "Rock.Mobile.Blocks.Core.Notes";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        public override object GetMobileConfigurationValues()
        {
            return new { };
        }

        #endregion

        private ICollection<NoteTypeCache> GetViewableNoteTypes()
        {
            return NoteTypeCache.All()
                .Where( a => !NoteTypes.Any() || NoteTypes.Contains( a.Guid ) )
                .Where( a => a.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                .ToList();
        }

        #region Action Methods

        [BlockAction]
        public BlockActionResult GetNotes( Guid? parentNoteGuid, int startIndex, int count )
        {
            using ( var rockContext = new RockContext() )
            {
                var noteService = new NoteService( rockContext );
                var viewableNoteTypeIds = GetViewableNoteTypes().Select( t => t.Id ).ToList();
                var baseUrl = GlobalAttributesCache.Value( "PublicApplicationRoot" );

                var notesQuery = noteService.Queryable()
                    .AsNoTracking()
                    .Include( a => a.CreatedByPersonAlias.Person )
                    .Include( a => a.ChildNotes )
                    .Where( a => viewableNoteTypeIds.Contains( a.NoteTypeId ) );


                if ( parentNoteGuid.HasValue )
                {
                    notesQuery = notesQuery.Where( a => a.ParentNote.Guid == parentNoteGuid.Value );
                }
                else
                {
                    var entityType = EntityTypeCache.Get( ContextEntityType );
                    var entity = entityType != null ? RequestContext.GetContextEntity( entityType.GetEntityType() ) : null;

                    if ( entity == null )
                    {
                        return ActionNotFound();
                    }

                    notesQuery = notesQuery.Where( a => a.EntityId == entity.Id && !a.ParentNoteId.HasValue );
                }

                var viewableNotes = notesQuery
                    .OrderByDescending( a => a.IsAlert == true )
                    .ThenByDescending( a => a.CreatedDateTime )
                    .ToList()
                    .Where( a => a.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                    .Skip( startIndex )
                    .Take( count )
                    .ToList();

                var noteData = viewableNotes
                    .Select( a => new
                    {
                        a.Guid,
                        a.Text,
                        PhotoUrl = a.CreatedByPersonAlias?.Person?.PhotoId != null ? $"{baseUrl}{a.CreatedByPersonAlias.Person.PhotoUrl}" : null,
                        Name = a.CreatedByPersonName,
                        Date = a.CreatedDateTime.HasValue ? ( DateTimeOffset? ) new DateTimeOffset( a.CreatedDateTime.Value ) : null,
                        ReplyCount = a.ChildNotes.Count( b => b.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                    } )
                    .ToList();

                return ActionOk( noteData );
            }
        }

        #endregion
    }
}
