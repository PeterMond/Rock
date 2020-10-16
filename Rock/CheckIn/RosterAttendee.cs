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
using System.Linq;
using System.Text;

using Rock.Model;

namespace Rock.CheckIn
{
    /// <summary>
    /// Attendance Information about a person when listed in an attendance roster
    /// </summary>
    public class RosterAttendee
    {
        private Person _person;

        /// <summary>
        /// Initializes a new instance of the <see cref="RosterAttendee"/> class.
        /// </summary>
        /// <param name="person">The person.</param>
        public RosterAttendee( Rock.Model.Person person )
        {
            person.LoadAttributes();

            _person = person;
            if ( person.AgeClassification != AgeClassification.Adult )
            {
                ParentNames = Rock.Model.Person.GetFamilySalutation( person, finalSeparator: "and" );
            }

            HasHealthNote = GetHasHealthNote( person );
            HasLegalNote = GetHasLegalNote( person );
        }

        #region Properties

        /// <summary>
        /// Gets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId => _person.Id;

        /// <summary>
        /// Gets the person unique identifier.
        /// </summary>
        /// <value>
        /// The person unique identifier.
        /// </value>
        public Guid PersonGuid => _person.Guid;

        /// <summary>
        /// Gets the attendance ids.
        /// </summary>
        /// <value>
        /// The attendance ids.
        /// </value>
        public List<int> AttendanceIds { get; private set; } = new List<int>();

        /// <summary>
        /// Gets the person's full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public string FullName => _person.FullName;

        /// <summary>
        /// Gets the parent names (if attendee is a child)
        /// </summary>
        /// <value>
        /// The parent names.
        /// </value>
        public string ParentNames { get; private set; }

        /// <summary>
        /// Gets the photo identifier.
        /// </summary>
        /// <value>
        /// The photo identifier.
        /// </value>
        public int? PhotoId => _person.PhotoId;

        /// <summary>
        /// Gets the age.
        /// </summary>
        /// <value>
        /// The age.
        /// </value>
        public int? Age => _person.Age;

        /// <summary>
        /// Gets the gender.
        /// </summary>
        /// <value>
        /// The gender.
        /// </value>
        public Gender Gender => _person.Gender;

        /// <summary>
        /// If the person has a birthday within the next 6 days (including today),
        /// returns the person's birthday (abbreviated day of week), 
        /// </summary>
        public string Birthday
        {
            get
            {
                if ( !IsBirthdayWeek )
                {
                    return null;
                }

                // If this Person's birthday is today, simply return "Today".
                int daysToBirthday = _person.DaysToBirthday;
                if ( daysToBirthday == 0 )
                {
                    return "Today";
                }

                return _person.BirthdayDayOfWeekShort;
            }
        }

        /// <summary>
        /// If the person has a birthday within the next 6 days (including today),
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is birthday week; otherwise, <c>false</c>.
        /// </value>
        public bool IsBirthdayWeek
        {
            get
            {
                return _person.DaysToBirthday < 7;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has health note.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has health note; otherwise, <c>false</c>.
        /// </value>
        public bool HasHealthNote { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has legal note.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has legal note; otherwise, <c>false</c>.
        /// </value>
        public bool HasLegalNote { get; private set; }

        /// <summary>
        /// Gets the unique tags.
        /// </summary>
        /// <value>
        /// The unique tags.
        /// </value>
        public List<string> UniqueTags { get; private set; } = new List<string>();

        /// <summary>
        /// Gets the attendee's <seealso cref="AttendanceCode"/> tag
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        public string Tag
        {
            get
            {
                return string.Join( ", ", UniqueTags );
            }
        }

        /// <summary>
        /// Gets the unique service times.
        /// </summary>
        /// <value>
        /// The unique service times.
        /// </value>
        public List<string> UniqueServiceTimes { get; private set; } = new List<string>();

        /// <summary>
        /// Gets the service times.
        /// </summary>
        /// <value>
        /// The service times.
        /// </value>
        public string ServiceTimes
        {
            get
            {
                return string.Join( ", ", UniqueServiceTimes );
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public RosterAttendeeStatus Status { get; private set; }

        /// <summary>
        /// Gets the status string.
        /// </summary>
        /// <value>
        /// The status string.
        /// </value>
        public string StatusString
        {
            get
            {
                return Status.GetDescription();
            }
        }

        /// <summary>
        /// Gets the check in time.
        /// </summary>
        /// <value>
        /// The check in time.
        /// </value>
        public DateTime CheckInTime { get; private set; }

        #endregion Properties

        #region HTML 

        /// <summary>
        /// Gets the person photo image tag.
        /// </summary>
        /// <returns></returns>
        public string GetPersonPhotoImageHtmlTag()
        {
            var imgTag = Rock.Model.Person.GetPersonPhotoImageTag( this.PersonId, this.PhotoId, this.Age, this.Gender, null, 50, 50, this.FullName, "avatar avatar-lg" );

            return imgTag;
        }

        /// <summary>
        /// Gets the status icon tag.
        /// </summary>
        /// <param name="isMobile">if set to <c>true</c> [is mobile].</param>
        /// <returns></returns>
        public string GetStatusIconHtmlTag( bool isMobile )
        {
            string statusClass = string.Empty;
            string mobileIcon = string.Empty;
            switch ( this.Status )
            {
                case RosterAttendeeStatus.CheckedIn:
                    statusClass = "warning";
                    mobileIcon = "&nbsp;";
                    break;
                case RosterAttendeeStatus.Present:
                    statusClass = "success";
                    mobileIcon = "<i class='fa fa-check'></i>";
                    break;
                case RosterAttendeeStatus.CheckedOut:
                    statusClass = "danger";
                    mobileIcon = "<i class='fa fa-minus'></i>";
                    break;
            }

            if ( isMobile )
            {
                return $"<span class='badge badge-circle badge-{statusClass}'>{mobileIcon}</span>";
            }
            else
            {
                return $"<span class='badge badge-{statusClass}'>{StatusString}</span>";
            }
        }

        /// <summary>
        /// Gets the attendee name HTML.
        /// </summary>
        /// <returns></returns>
        public string GetAttendeeNameHtml()
        {
            var result = $@"
<div class='name'>
    <span class='js-checkin-person-name'>{this.FullName}</span>
     <span class='badges d-sm-none'>{this.GetBadgesHtml( true )}</span>
</div>
<div class='parent-name small text-muted'>{this.ParentNames}</div>";

            return result;
        }

        /// <summary>
        /// Gets the mobile tag and schedules HTML.
        /// </summary>
        /// <returns></returns>
        public string GetMobileTagAndSchedulesHtml()
        {
            return $"<div class='person-tag'>{this.Tag}</div><div class='small text-muted'>{this.ServiceTimes}</div>";
        }

        /// <summary>
        /// Gets the badges markup.
        /// </summary>
        /// <param name="isMobile">if set to <c>true</c> [is mobile].</param>
        /// <returns></returns>
        public string GetBadgesHtml( bool isMobile )
        {
            var badgesSb = new StringBuilder();

            if ( this.IsBirthdayWeek )
            {
                if ( isMobile )
                {
                    badgesSb.Append( "&nbsp;<i class='fa fa-birthday-cake text-success'></i>" );
                }
                else
                {
                    badgesSb.Append( $"<div class='text-center text-success pull-left'><div><i class='fa fa-birthday-cake fa-2x'></i></div><div style='font-size: small;'>{this.Birthday}</div></div>" );
                }
            }

            var openDiv = isMobile ? string.Empty : "<div class='pull-left'>";
            var closeDiv = isMobile ? string.Empty : "</div>";
            var fa2x = isMobile ? string.Empty : " fa-2x";

            if ( this.HasHealthNote )
            {
                badgesSb.Append( $"{openDiv}&nbsp;<i class='fa fa-notes-medical{fa2x} text-danger'></i>{openDiv}" );
            }

            if ( this.HasLegalNote )
            {
                badgesSb.Append( $"{openDiv}&nbsp;<i class='fa fa-clipboard{fa2x}'></i>{closeDiv}" );
            }

            return badgesSb.ToString();
        }

        #endregion HTML

        #region Private methods

        /// <summary>
        /// Sets the attendance-specific properties.
        /// </summary>
        /// <param name="attendance">The attendance.</param>
        private void SetAttendanceInfo( Attendance attendance )
        {
            RosterAttendee attendee = this;

            // Keep track of each Attendance ID tied to this Attendee so we can manage them all as a group.
            attendee.AttendanceIds.Add( attendance.Id );

            // Tag(s).
            string tag = attendance.AttendanceCode?.Code;

            if ( tag.IsNotNullOrWhiteSpace() && !attendee.UniqueTags.Contains( tag, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueTags.Add( tag );
            }

            // Service Time(s).
            string serviceTime = attendance.Occurrence?.Schedule?.Name;

            if ( serviceTime.IsNotNullOrWhiteSpace() && !attendee.UniqueServiceTimes.Contains( serviceTime, StringComparer.OrdinalIgnoreCase ) )
            {
                attendee.UniqueServiceTimes.Add( serviceTime );
            }

            // Status: if this Attendee has multiple AttendanceOccurrences, the highest AttendeeStatus value among them wins.
            RosterAttendeeStatus attendeeStatus = RosterAttendeeStatus.CheckedIn;
            if ( attendance.EndDateTime.HasValue )
            {
                attendeeStatus = RosterAttendeeStatus.CheckedOut;
            }
            else if ( attendance.PresentDateTime.HasValue )
            {
                attendeeStatus = RosterAttendeeStatus.Present;
            }

            if ( attendeeStatus > attendee.Status )
            {
                attendee.Status = attendeeStatus;
            }

            // Check-in Time: if this Attendee has multiple AttendanceOccurrences, the latest StartDateTime value among them wins.
            if ( attendance.StartDateTime > attendee.CheckInTime )
            {
                attendee.CheckInTime = attendance.StartDateTime;
            }
        }

        /// <summary>
        /// Gets whether the person has a health note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasHealthNote( Rock.Model.Person person )
        {
            const string Person_Allergy = "Allergy";
            string attributeValue = person.GetAttributeValue( Person_Allergy );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        /// <summary>
        /// Gets whether the person has a legal note.
        /// </summary>
        /// <param name="person">The person.</param>
        private bool GetHasLegalNote( Rock.Model.Person person )
        {
            const string Person_LegalNotes = "LegalNotes";
            string attributeValue = person.GetAttributeValue( Person_LegalNotes );
            return attributeValue.IsNotNullOrWhiteSpace();
        }

        #endregion Private methods

        #region Static methods

        /// <summary>
        /// Returns a list of <see cref="RosterAttendee"/> from the attendance list
        /// </summary>
        /// <param name="attendanceList">The attendance list.</param>
        /// <returns></returns>
        public static IList<RosterAttendee> GetFromAttendanceList( IList<Attendance> attendanceList )
        {
            var attendees = new List<RosterAttendee>();
            foreach ( var attendance in attendanceList )
            {
                // Create an Attendee for each unique Person within the Attendance records.
                var person = attendance.PersonAlias.Person;

                RosterAttendee attendee = attendees.FirstOrDefault( a => a.PersonGuid == person.Guid );
                if ( attendee == null )
                {
                    attendee = new RosterAttendee( person );
                    attendees.Add( attendee );
                }

                // Add the attendance-specific property values.
                attendee.SetAttendanceInfo( attendance );
            }

            return attendees;
        }

        #endregion Static methods
    }

    /// <summary>
    /// The status of an attendee.
    /// </summary>
    public enum RosterAttendeeStatus
    {
        /// <summary>
        /// Checked in
        /// </summary>
        [Description( "Checked-in" )]
        CheckedIn = 1,

        /// <summary>
        /// Present
        /// </summary>
        [Description( "Present" )]
        Present = 2,

        /// <summary>
        /// Checked out
        /// </summary>
        [Description( "Checked-out" )]
        CheckedOut = 3
    }
}