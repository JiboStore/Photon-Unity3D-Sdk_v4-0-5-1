using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
    class WebRpcResponse
    {
        public string Name { get; private set; }
        /// <summary>-1 tells you: Got not ReturnCode from WebRpc service.</summary>
        public int ReturnCode { get; private set; }
        public string DebugMessage { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }

        public WebRpcResponse(OperationResponse response)
        {
            object value;
            response.Parameters.TryGetValue(ParameterCode.UriPath, out value);
            this.Name = value as string;
            
            response.Parameters.TryGetValue(ParameterCode.WebRpcReturnCode, out value);
            this.ReturnCode = (value != null) ? (byte)value : -1;

            response.Parameters.TryGetValue(ParameterCode.WebRpcParameters, out value);
            this.Parameters = value as Dictionary<string, object>;

            response.Parameters.TryGetValue(ParameterCode.WebRpcReturnMessage, out value);
            this.DebugMessage = value as string;
        }

        public string ToStringFull()
        {
            return string.Format("{0}={2}: {1} \"{3}\"", Name, SupportClass.DictionaryToString(Parameters), ReturnCode, DebugMessage);
        }
    }
}
