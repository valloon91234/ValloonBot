/* 
 * BitMEX API
 *
 * ## REST API for the BitMEX Trading Platform  [View Changelog](/app/apiChangelog)  -  #### Getting Started  Base URI: [https://www.bitmex.com/api/v1](/api/v1)  ##### Fetching Data  All REST endpoints are documented below. You can try out any query right from this interface.  Most table queries accept `count`, `start`, and `reverse` params. Set `reverse=true` to get rows newest-first.  Additional documentation regarding filters, timestamps, and authentication is available in [the main API documentation](/app/restAPI).  _All_ table data is available via the [Websocket](/app/wsAPI). We highly recommend using the socket if you want to have the quickest possible data without being subject to ratelimits.  ##### Return Types  By default, all data is returned as JSON. Send `?_format=csv` to get CSV data or `?_format=xml` to get XML data.  ##### Trade Data Queries  _This is only a small subset of what is available, to get you started._  Fill in the parameters and click the `Try it out!` button to try any of these queries.  - [Pricing Data](#!/Quote/Quote_get)  - [Trade Data](#!/Trade/Trade_get)  - [OrderBook Data](#!/OrderBook/OrderBook_getL2)  - [Settlement Data](#!/Settlement/Settlement_get)  - [Exchange Statistics](#!/Stats/Stats_history)  Every function of the BitMEX.com platform is exposed here and documented. Many more functions are available.  ##### Swagger Specification  [⇩ Download Swagger JSON](swagger.json)  -  ## All API Endpoints  Click to expand a section. 
 *
 * OpenAPI spec version: 1.2.0
 * Contact: support@bitmex.com
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using SwaggerDateConverter = IO.Swagger.Client.SwaggerDateConverter;

namespace IO.Swagger.Model
{
    /// <summary>
    /// User Events for auditing
    /// </summary>
    [DataContract]
    public partial class UserEvent :  IEquatable<UserEvent>, IValidatableObject
    {
        /// <summary>
        /// Defines Type
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            
            /// <summary>
            /// Enum ApiKeyCreated for value: apiKeyCreated
            /// </summary>
            [EnumMember(Value = "apiKeyCreated")]
            ApiKeyCreated = 1,
            
            /// <summary>
            /// Enum DeleverageExecution for value: deleverageExecution
            /// </summary>
            [EnumMember(Value = "deleverageExecution")]
            DeleverageExecution = 2,
            
            /// <summary>
            /// Enum DepositConfirmed for value: depositConfirmed
            /// </summary>
            [EnumMember(Value = "depositConfirmed")]
            DepositConfirmed = 3,
            
            /// <summary>
            /// Enum DepositPending for value: depositPending
            /// </summary>
            [EnumMember(Value = "depositPending")]
            DepositPending = 4,
            
            /// <summary>
            /// Enum BanZeroVolumeApiUser for value: banZeroVolumeApiUser
            /// </summary>
            [EnumMember(Value = "banZeroVolumeApiUser")]
            BanZeroVolumeApiUser = 5,
            
            /// <summary>
            /// Enum LiquidationOrderPlaced for value: liquidationOrderPlaced
            /// </summary>
            [EnumMember(Value = "liquidationOrderPlaced")]
            LiquidationOrderPlaced = 6,
            
            /// <summary>
            /// Enum Login for value: login
            /// </summary>
            [EnumMember(Value = "login")]
            Login = 7,
            
            /// <summary>
            /// Enum PgpMaskedEmail for value: pgpMaskedEmail
            /// </summary>
            [EnumMember(Value = "pgpMaskedEmail")]
            PgpMaskedEmail = 8,
            
            /// <summary>
            /// Enum PgpTestEmail for value: pgpTestEmail
            /// </summary>
            [EnumMember(Value = "pgpTestEmail")]
            PgpTestEmail = 9,
            
            /// <summary>
            /// Enum PasswordChanged for value: passwordChanged
            /// </summary>
            [EnumMember(Value = "passwordChanged")]
            PasswordChanged = 10,
            
            /// <summary>
            /// Enum PositionStateLiquidated for value: positionStateLiquidated
            /// </summary>
            [EnumMember(Value = "positionStateLiquidated")]
            PositionStateLiquidated = 11,
            
            /// <summary>
            /// Enum PositionStateWarning for value: positionStateWarning
            /// </summary>
            [EnumMember(Value = "positionStateWarning")]
            PositionStateWarning = 12,
            
            /// <summary>
            /// Enum ResetPasswordConfirmed for value: resetPasswordConfirmed
            /// </summary>
            [EnumMember(Value = "resetPasswordConfirmed")]
            ResetPasswordConfirmed = 13,
            
            /// <summary>
            /// Enum ResetPasswordRequest for value: resetPasswordRequest
            /// </summary>
            [EnumMember(Value = "resetPasswordRequest")]
            ResetPasswordRequest = 14,
            
            /// <summary>
            /// Enum TransferCanceled for value: transferCanceled
            /// </summary>
            [EnumMember(Value = "transferCanceled")]
            TransferCanceled = 15,
            
            /// <summary>
            /// Enum TransferCompleted for value: transferCompleted
            /// </summary>
            [EnumMember(Value = "transferCompleted")]
            TransferCompleted = 16,
            
            /// <summary>
            /// Enum TransferReceived for value: transferReceived
            /// </summary>
            [EnumMember(Value = "transferReceived")]
            TransferReceived = 17,
            
            /// <summary>
            /// Enum TransferRequested for value: transferRequested
            /// </summary>
            [EnumMember(Value = "transferRequested")]
            TransferRequested = 18,
            
            /// <summary>
            /// Enum TwoFactorDisabled for value: twoFactorDisabled
            /// </summary>
            [EnumMember(Value = "twoFactorDisabled")]
            TwoFactorDisabled = 19,
            
            /// <summary>
            /// Enum TwoFactorEnabled for value: twoFactorEnabled
            /// </summary>
            [EnumMember(Value = "twoFactorEnabled")]
            TwoFactorEnabled = 20,
            
            /// <summary>
            /// Enum WithdrawalCanceled for value: withdrawalCanceled
            /// </summary>
            [EnumMember(Value = "withdrawalCanceled")]
            WithdrawalCanceled = 21,
            
            /// <summary>
            /// Enum WithdrawalCompleted for value: withdrawalCompleted
            /// </summary>
            [EnumMember(Value = "withdrawalCompleted")]
            WithdrawalCompleted = 22,
            
            /// <summary>
            /// Enum WithdrawalConfirmed for value: withdrawalConfirmed
            /// </summary>
            [EnumMember(Value = "withdrawalConfirmed")]
            WithdrawalConfirmed = 23,
            
            /// <summary>
            /// Enum WithdrawalRequested for value: withdrawalRequested
            /// </summary>
            [EnumMember(Value = "withdrawalRequested")]
            WithdrawalRequested = 24,
            
            /// <summary>
            /// Enum Verify for value: verify
            /// </summary>
            [EnumMember(Value = "verify")]
            Verify = 25
        }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [DataMember(Name="type", EmitDefaultValue=false)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Defines Status
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        {
            
            /// <summary>
            /// Enum Success for value: success
            /// </summary>
            [EnumMember(Value = "success")]
            Success = 1,
            
            /// <summary>
            /// Enum Failure for value: failure
            /// </summary>
            [EnumMember(Value = "failure")]
            Failure = 2
        }

        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name="status", EmitDefaultValue=false)]
        public StatusEnum Status { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserEvent" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected UserEvent() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UserEvent" /> class.
        /// </summary>
        /// <param name="id">id.</param>
        /// <param name="type">type (required).</param>
        /// <param name="status">status (required).</param>
        /// <param name="userId">userId (required).</param>
        /// <param name="createdById">createdById (required).</param>
        /// <param name="ip">ip.</param>
        /// <param name="geoipCountry">geoipCountry.</param>
        /// <param name="geoipRegion">geoipRegion.</param>
        /// <param name="geoipSubRegion">geoipSubRegion.</param>
        /// <param name="eventMeta">eventMeta.</param>
        /// <param name="created">created (required).</param>
        public UserEvent(double? id = default, TypeEnum type = default, StatusEnum status = default, double? userId = default, double? createdById = default, string ip = default, string geoipCountry = default, string geoipRegion = default, string geoipSubRegion = default, Object eventMeta = default, DateTime? created = default)
        {
                this.Type = type;
                this.Status = status;
            // to ensure "userId" is required (not null)
            if (userId == null)
            {
                throw new InvalidDataException("userId is a required property for UserEvent and cannot be null");
            }
            else
            {
                this.UserId = userId;
            }
            // to ensure "createdById" is required (not null)
            if (createdById == null)
            {
                throw new InvalidDataException("createdById is a required property for UserEvent and cannot be null");
            }
            else
            {
                this.CreatedById = createdById;
            }
            // to ensure "created" is required (not null)
            if (created == null)
            {
                throw new InvalidDataException("created is a required property for UserEvent and cannot be null");
            }
            else
            {
                this.Created = created;
            }
            this.Id = id;
            this.Ip = ip;
            this.GeoipCountry = geoipCountry;
            this.GeoipRegion = geoipRegion;
            this.GeoipSubRegion = geoipSubRegion;
            this.EventMeta = eventMeta;
        }
        
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name="id", EmitDefaultValue=false)]
        public double? Id { get; set; }



        /// <summary>
        /// Gets or Sets UserId
        /// </summary>
        [DataMember(Name="userId", EmitDefaultValue=false)]
        public double? UserId { get; set; }

        /// <summary>
        /// Gets or Sets CreatedById
        /// </summary>
        [DataMember(Name="createdById", EmitDefaultValue=false)]
        public double? CreatedById { get; set; }

        /// <summary>
        /// Gets or Sets Ip
        /// </summary>
        [DataMember(Name="ip", EmitDefaultValue=false)]
        public string Ip { get; set; }

        /// <summary>
        /// Gets or Sets GeoipCountry
        /// </summary>
        [DataMember(Name="geoipCountry", EmitDefaultValue=false)]
        public string GeoipCountry { get; set; }

        /// <summary>
        /// Gets or Sets GeoipRegion
        /// </summary>
        [DataMember(Name="geoipRegion", EmitDefaultValue=false)]
        public string GeoipRegion { get; set; }

        /// <summary>
        /// Gets or Sets GeoipSubRegion
        /// </summary>
        [DataMember(Name="geoipSubRegion", EmitDefaultValue=false)]
        public string GeoipSubRegion { get; set; }

        /// <summary>
        /// Gets or Sets EventMeta
        /// </summary>
        [DataMember(Name="eventMeta", EmitDefaultValue=false)]
        public Object EventMeta { get; set; }

        /// <summary>
        /// Gets or Sets Created
        /// </summary>
        [DataMember(Name="created", EmitDefaultValue=false)]
        public DateTime? Created { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class UserEvent {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  UserId: ").Append(UserId).Append("\n");
            sb.Append("  CreatedById: ").Append(CreatedById).Append("\n");
            sb.Append("  Ip: ").Append(Ip).Append("\n");
            sb.Append("  GeoipCountry: ").Append(GeoipCountry).Append("\n");
            sb.Append("  GeoipRegion: ").Append(GeoipRegion).Append("\n");
            sb.Append("  GeoipSubRegion: ").Append(GeoipSubRegion).Append("\n");
            sb.Append("  EventMeta: ").Append(EventMeta).Append("\n");
            sb.Append("  Created: ").Append(Created).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as UserEvent);
        }

        /// <summary>
        /// Returns true if UserEvent instances are equal
        /// </summary>
        /// <param name="input">Instance of UserEvent to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(UserEvent input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.Type == input.Type ||
                    (this.Type.Equals(input.Type))
                ) && 
                (
                    this.Status == input.Status ||
                    (this.Status.Equals(input.Status))
                ) && 
                (
                    this.UserId == input.UserId ||
                    (this.UserId != null &&
                    this.UserId.Equals(input.UserId))
                ) && 
                (
                    this.CreatedById == input.CreatedById ||
                    (this.CreatedById != null &&
                    this.CreatedById.Equals(input.CreatedById))
                ) && 
                (
                    this.Ip == input.Ip ||
                    (this.Ip != null &&
                    this.Ip.Equals(input.Ip))
                ) && 
                (
                    this.GeoipCountry == input.GeoipCountry ||
                    (this.GeoipCountry != null &&
                    this.GeoipCountry.Equals(input.GeoipCountry))
                ) && 
                (
                    this.GeoipRegion == input.GeoipRegion ||
                    (this.GeoipRegion != null &&
                    this.GeoipRegion.Equals(input.GeoipRegion))
                ) && 
                (
                    this.GeoipSubRegion == input.GeoipSubRegion ||
                    (this.GeoipSubRegion != null &&
                    this.GeoipSubRegion.Equals(input.GeoipSubRegion))
                ) && 
                (
                    this.EventMeta == input.EventMeta ||
                    (this.EventMeta != null &&
                    this.EventMeta.Equals(input.EventMeta))
                ) && 
                (
                    this.Created == input.Created ||
                    (this.Created != null &&
                    this.Created.Equals(input.Created))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Id != null)
                    hashCode = hashCode * 59 + this.Id.GetHashCode();
                //if (this.Type != null)
                    hashCode = hashCode * 59 + this.Type.GetHashCode();
                //if (this.Status != null)
                    hashCode = hashCode * 59 + this.Status.GetHashCode();
                if (this.UserId != null)
                    hashCode = hashCode * 59 + this.UserId.GetHashCode();
                if (this.CreatedById != null)
                    hashCode = hashCode * 59 + this.CreatedById.GetHashCode();
                if (this.Ip != null)
                    hashCode = hashCode * 59 + this.Ip.GetHashCode();
                if (this.GeoipCountry != null)
                    hashCode = hashCode * 59 + this.GeoipCountry.GetHashCode();
                if (this.GeoipRegion != null)
                    hashCode = hashCode * 59 + this.GeoipRegion.GetHashCode();
                if (this.GeoipSubRegion != null)
                    hashCode = hashCode * 59 + this.GeoipSubRegion.GetHashCode();
                if (this.EventMeta != null)
                    hashCode = hashCode * 59 + this.EventMeta.GetHashCode();
                if (this.Created != null)
                    hashCode = hashCode * 59 + this.Created.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            // GeoipCountry (string) maxLength
            if(this.GeoipCountry != null && this.GeoipCountry.Length > 2)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for GeoipCountry, length must be less than 2.", new [] { "GeoipCountry" });
            }

            // GeoipRegion (string) maxLength
            if(this.GeoipRegion != null && this.GeoipRegion.Length > 3)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for GeoipRegion, length must be less than 3.", new [] { "GeoipRegion" });
            }

            // GeoipSubRegion (string) maxLength
            if(this.GeoipSubRegion != null && this.GeoipSubRegion.Length > 3)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for GeoipSubRegion, length must be less than 3.", new [] { "GeoipSubRegion" });
            }

            yield break;
        }
    }

}
