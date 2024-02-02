using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace APIGw_Lambda_SQS;

public class Function
{

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        string messageID = string.Empty;

        if (apigProxyEvent.HttpMethod != "POST" || string.IsNullOrEmpty(apigProxyEvent.Body))
        {
            string errorText = "{ \"Error\" : \"Invalid HttpMethod or empty request body!\" }";
            Console.WriteLine(errorText);
            return GenerateAndReturnResponse((int)HttpStatusCode.BadRequest, errorText);
        }

        Console.WriteLine("Valid API Gateway event received: " + apigProxyEvent.Body);

        try
        {
            messageID = await SendMessageToSQS(apigProxyEvent.Body);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

            string errorText = "{ \"Error\" : \"" + ex.Message + "\" }";


            return GenerateAndReturnResponse((int)HttpStatusCode.InternalServerError, errorText);
        }

        return GenerateAndReturnResponse((int)HttpStatusCode.OK, "{ \"MessageID\" : \"" + messageID + "\" }");
    }

    /// <summary>
    /// Note: Lambda functions IAM Role must have permission to send message to SQS
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task<string> SendMessageToSQS (string message)
    {
        var sqs = new AmazonSQSClient();
        var queueUrl = "YOUR_SQS_URL_HERE";

        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = message
        };

        var sendMessageResponse = await sqs.SendMessageAsync(sendMessageRequest);

        return sendMessageResponse.MessageId;
    }

    private APIGatewayProxyResponse GenerateAndReturnResponse(int statusCode, string jsonMessage)
    {
        return new APIGatewayProxyResponse
        {
            Body = jsonMessage,
            StatusCode = statusCode,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
