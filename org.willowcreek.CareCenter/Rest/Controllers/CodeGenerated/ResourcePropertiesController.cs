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

using org.willowcreek.CareCenter.Model;

namespace org.willowcreek.CareCenter.Rest.Controllers
{
    /// <summary>
    /// ResourceProperties REST API
    /// </summary>
    public partial class ResourcePropertiesController : Rock.Rest.ApiController<org.willowcreek.CareCenter.Model.ResourceProperty>
    {
        public ResourcePropertiesController() : base( new org.willowcreek.CareCenter.Model.ResourcePropertyService( new Rock.Data.RockContext() ) ) { } 
    }
}
