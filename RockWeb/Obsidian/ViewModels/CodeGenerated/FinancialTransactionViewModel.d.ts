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

export default interface FinancialTransaction extends Entity {
    Id: number;
    AuthorizedPersonAliasId: number | null;
    BatchId: number | null;
    CheckMicrEncrypted: string | null;
    CheckMicrHash: string | null;
    CheckMicrParts: string | null;
    FinancialGatewayId: number | null;
    FinancialPaymentDetailId: number | null;
    ForeignGuid: Guid | null;
    ForeignKey: string | null;
    FutureProcessingDateTime: string | Date | null;
    IsReconciled: boolean | null;
    IsSettled: boolean | null;
    MICRStatus: number | null;
    NonCashAssetTypeValueId: number | null;
    ProcessedByPersonAliasId: number | null;
    ProcessedDateTime: string | Date | null;
    ScheduledTransactionId: number | null;
    SettledDate: string | Date | null;
    SettledGroupId: string | null;
    ShowAsAnonymous: boolean;
    SourceTypeValueId: number | null;
    Status: string | null;
    StatusMessage: string | null;
    Summary: string | null;
    SundayDate: string | Date | null;
    TransactionCode: string | null;
    TransactionDateTime: string | Date | null;
    TransactionTypeValueId: number;
    CreatedDateTime: string | Date | null;
    ModifiedDateTime: string | Date | null;
    CreatedByPersonAliasId: number | null;
    ModifiedByPersonAliasId: number | null;
    Guid: Guid;
    ForeignId: number | null;
}