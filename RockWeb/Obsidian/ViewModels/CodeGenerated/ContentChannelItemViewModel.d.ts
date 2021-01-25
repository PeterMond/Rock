//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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

import Entity from '../Entity.js';
import { Guid } from '../../Util/Guid.js';

export default interface ContentChannelItem extends Entity {
    Id: number;
    ApprovedByPersonAliasId: number | null;
    ApprovedDateTime: string | Date | null;
    Content: string | null;
    ContentChannelId: number;
    ContentChannelTypeId: number;
    ExpireDateTime: string | Date | null;
    ForeignGuid: Guid | null;
    ForeignKey: string | null;
    ItemGlobalKey: string | null;
    Order: number;
    Permalink: string | null;
    Priority: number;
    StartDateTime: string | Date;
    Status: number;
    StructuredContent: string | null;
    Title: string | null;
    CreatedDateTime: string | Date | null;
    ModifiedDateTime: string | Date | null;
    CreatedByPersonAliasId: number | null;
    ModifiedByPersonAliasId: number | null;
    Guid: Guid;
    ForeignId: number | null;
}