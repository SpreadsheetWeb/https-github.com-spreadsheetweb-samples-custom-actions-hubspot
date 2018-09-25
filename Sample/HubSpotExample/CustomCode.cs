using Pagos.Designer.Interfaces.External.CustomHooks;
using Pagos.Designer.Interfaces.External.Messaging;
using Pagos.SpreadsheetWeb.Web.Api.Objects.Calculation;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HubSpotExample
{
    /// <summary>
    /// The custom code class, implementing relevant interfaces for desired 
    /// actions (i.e. AfterCalculation in this scenario).
    /// </summary>
    public class CustomCode : IAfterCalculation
    {
        public const string apiKey = "your-hubspot-api-key";

        /// <summary>
        /// After calculation occurs in the calculation engine, provide a hook
        /// to perform additional custom actions.
        /// </summary>
        /// <param name="request">The request that was sent to the calculation engine.</param>
        /// <param name="response">The response that came back from the calculation engine.</param>
        /// <returns></returns>
        public ActionableResponse AfterCalculation(CalculationRequest request, CalculationResponse response)
        {
            try
            {
                // Set up the REST client and create the POST request to the web service with JSON formatting.
                var client = new RestClient("https://api.hubapi.com/");
                var restReq =
                    new RestRequest("contacts/v1/contact/createOrUpdate/email/{email}", Method.POST)
                    {
                        RequestFormat = DataFormat.Json
                    };

                // Set the API key in the request's query params.
                restReq.AddQueryParameter("hapikey", apiKey);

                // From the SpreadsheetWEB calculation request, grab the 'iName' and 'iEmail' inputs.
                var name = request.Inputs.FirstOrDefault(x => x.Ref == "iName");
                var emailAddress = request.Inputs.FirstOrDefault(x => x.Ref == "iEmail");

                // Set up the email parameter in the target endpoint URL.
                restReq.AddUrlSegment("email", emailAddress?.Value[0][0].Value);

                // Generate the body of the request, submitting the two name properties.
                restReq.AddBody(new
                {
                    properties = new[]
                    {
                        // For sake of simplicity in the user interface, we are sending 
                        // only a single name as both first and last name. Of course, 
                        // in a true implementation you would set these accordingly.
                        new { property = "firstname", value = name?.Value[0][0].Value },
                        new { property = "lastname", value = name?.Value[0][0].Value }
                    }
                });

                // Execute the request.
                var restResp = client.Execute(restReq);
                if (!restResp.IsSuccessful)
                {
                    throw new Exception(restResp.ErrorMessage);
                }

                // for demonstration purpose we can set the StatusDescription ofmade request
                response.Outputs.FirstOrDefault(x => x.Ref == "Response").Value[0][0].Value = restResp.StatusDescription;
            }
            catch (Exception ex)
            {
                // If something goes wrong, cancel the rest of the processing and return
                // the message to the user interface.
                return new ActionableResponse
                {
                    Success = false,
                    ResponseMessages = new List<ResponseMessage>
                    {
                        new ResponseMessage
                        {
                            Message = ex.Message,
                            MessageLevel = MessageLevel.Danger
                        }
                    }
                };
            }

            // If all went well, indicate that the process was successful.
            return new ActionableResponse
            {
                Success = true
            };
        }
    }
}