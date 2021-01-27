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
            var contextEntityType = EntityTypeCache.Get( ContextEntityType );

            if ( contextEntityType == null )
            {
                return new NoteTypeCache[0];
            }

            return NoteTypeCache.GetByEntity( contextEntityType.Id, string.Empty, string.Empty, true )
                .Where( a => !NoteTypes.Any() || NoteTypes.Contains( a.Guid ) )
                .Where( a => a.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                .ToList();
        }

        private ICollection<NoteTypeCache> GetEditableNoteTypes()
        {
            var contextEntityType = EntityTypeCache.Get( ContextEntityType );

            if ( contextEntityType == null )
            {
                return new NoteTypeCache[0];
            }

            return NoteTypeCache.GetByEntity( contextEntityType.Id, string.Empty, string.Empty, true )
                .Where( a => !NoteTypes.Any() || NoteTypes.Contains( a.Guid ) )
                .Where( a => a.UserSelectable )
                .Where( a => a.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                .ToList();
        }

        private object GetNoteObject( Note note )
        {
            var baseUrl = GlobalAttributesCache.Value( "PublicApplicationRoot" );
            var canEdit = note.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );

            return new
            {
                note.Guid,
                NoteTypeGuid = note.NoteType.Guid,
                note.Text,
                PhotoUrl = note.CreatedByPersonAlias?.Person?.PhotoId != null ? $"{baseUrl}{note.CreatedByPersonAlias.Person.PhotoUrl}" : null,
                Name = note.CreatedByPersonName,
                Date = note.CreatedDateTime.HasValue ? ( DateTimeOffset? ) new DateTimeOffset( note.CreatedDateTime.Value ) : null,
                ReplyCount = note.ChildNotes.Count( b => b.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) ),
                CanEdit = canEdit,
                CanDelete = canEdit
            };
        }

        private List<object> GetEntityNotes( Guid? parentNoteGuid, int startIndex, int count )
        {
            using ( var rockContext = new RockContext() )
            {
                var noteService = new NoteService( rockContext );
                var viewableNoteTypeIds = GetViewableNoteTypes().Select( t => t.Id ).ToList();

                var entityType = EntityTypeCache.Get( ContextEntityType );
                var entity = entityType != null ? RequestContext.GetContextEntity( entityType.GetEntityType() ) : null;
                if ( entity == null )
                {
                    // Indicate to caller "not found" error.
                    return null;
                }

                var notesQuery = noteService.Queryable()
                    .AsNoTracking()
                    .Include( a => a.CreatedByPersonAlias.Person )
                    .Include( a => a.ChildNotes )
                    .Where( a => viewableNoteTypeIds.Contains( a.NoteTypeId ) )
                    .Where( a => a.EntityId == entity.Id );

                if ( parentNoteGuid.HasValue )
                {
                    notesQuery = notesQuery.Where( a => a.ParentNote.Guid == parentNoteGuid.Value );
                }
                else
                {
                    notesQuery = notesQuery.Where( a => !a.ParentNoteId.HasValue );
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
                    .Select( a => GetNoteObject( a ) )
                    .ToList();

                return noteData;
            }
        }

        #region Action Methods

        [BlockAction]
        public BlockActionResult GetInitialData( int count )
        {
            var notes = GetEntityNotes( null, 0, count );

            if ( notes == null )
            {
                return ActionNotFound();
            }

            var editableNoteTypes = GetEditableNoteTypes()
                .Select( a => new
                {
                    a.Guid,
                    a.Name,
                    a.UserSelectable
                } );

            return ActionOk( new
            {
                EditableNoteTypes = editableNoteTypes,
                Notes = notes
            } );
        }

        [BlockAction]
        public BlockActionResult GetNotes( Guid? parentNoteGuid, int startIndex, int count )
        {
            var notes = GetEntityNotes( parentNoteGuid, startIndex, count );

            if ( notes == null )
            {
                return ActionNotFound();
            }

            return ActionOk( notes );
        }

        [BlockAction]
        public BlockActionResult SaveNote( Guid? noteGuid, Guid? parentNoteGuid, Guid noteTypeGuid, string text, bool isAlert, bool isPrivate )
        {
            var noteType = NoteTypeCache.Get( noteTypeGuid );

            if ( noteType == null )
            {
                return ActionBadRequest( "Invalid note type." );
            }

            var entityType = EntityTypeCache.Get( ContextEntityType );
            var entity = entityType != null ? RequestContext.GetContextEntity( entityType.GetEntityType() ) : null;
            if ( entity == null )
            {
                return ActionBadRequest( "Unknown note type." );
            }

            using ( var rockContext = new RockContext() )
            {
                var noteService = new NoteService( rockContext );
                Note note;

                var parentNote = parentNoteGuid.HasValue ? noteService.Get( parentNoteGuid.Value ) : null;

                if ( !noteGuid.HasValue )
                {
                    if ( !noteType.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                    {
                        return ActionForbidden( "Not authorized to add note." );
                    }

                    note = rockContext.Notes.Create();
                    note.IsSystem = false;
                    note.EntityId = entity.Id;
                    note.ParentNoteId = parentNote?.Id;

                    noteService.Add( note );
                }
                else
                {
                    note = noteService.Get( noteGuid.Value );

                    if ( note == null )
                    {
                        return ActionNotFound();
                    }

                    if ( !note.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                    {
                        return ActionForbidden( "Not authorized to edit note." );
                    }
                }

                // If the note is new or is owned by the current person then
                // update the private flag.
                if ( note.Id == 0 || ( note.CreatedByPersonId.HasValue && RequestContext.CurrentPerson?.Id == note.CreatedByPersonId ) )
                {
                    note.IsPrivateNote = isPrivate;
                }

                // It's up to the client to handle logic for non-user selectable
                // note types.
                note.NoteTypeId = noteType.Id;

                string personalNoteCaption = "You - Personal Note";
                if ( string.IsNullOrWhiteSpace( note.Caption ) )
                {
                    note.Caption = note.IsPrivateNote ? personalNoteCaption : string.Empty;
                }
                else
                {
                    // if the note still has the personalNoteCaption, but was changed to have IsPrivateNote to false, change the caption to empty string
                    if ( note.Caption == personalNoteCaption && !note.IsPrivateNote )
                    {
                        note.Caption = string.Empty;
                    }
                }

                note.Text = text;
                note.IsAlert = isAlert;

                note.EditedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
                note.EditedDateTime = RockDateTime.Now;

                if ( noteType.RequiresApprovals )
                {
                    if ( note.IsAuthorized( Authorization.APPROVE, RequestContext.CurrentPerson ) )
                    {
                        note.ApprovalStatus = NoteApprovalStatus.Approved;
                        note.ApprovedByPersonAliasId = RequestContext.CurrentPerson?.PrimaryAliasId;
                        note.ApprovedDateTime = RockDateTime.Now;
                    }
                    else
                    {
                        note.ApprovalStatus = NoteApprovalStatus.PendingApproval;
                    }
                }
                else
                {
                    note.ApprovalStatus = NoteApprovalStatus.Approved;
                }

                rockContext.SaveChanges();

                return ActionOk( GetNoteObject( note ) );
            }
        }

        [BlockAction]
        public BlockActionResult DeleteNote( Guid noteGuid )
        {
            using ( var rockContext = new RockContext() )
            {
                var service = new NoteService( rockContext );
                var note = service.Get( noteGuid );

                if ( note == null )
                {
                    return ActionNotFound();
                }

                if ( !note.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                {
                    // Rock.Constants strings include HTML so don't use.
                    return ActionForbidden( "You are not authorized to delete this note." );
                }

                if ( service.CanDeleteChildNotes( note, RequestContext.CurrentPerson, out var errorMessage ) && service.CanDelete( note, out errorMessage ) )
                {
                    service.Delete( note, true );
                    rockContext.SaveChanges();

                    return ActionOk();
                }
                else
                {
                    return ActionForbidden( errorMessage );
                }
            }
        }

        #endregion
    }
}
