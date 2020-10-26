using System.Collections.Generic;
using STak.TakHub.Core.Interfaces;

namespace STak.TakHub.Core.Dto.UseCaseResponses
{
    public class LogoutResponse : UseCaseResponseMessage
    {
        public IEnumerable<Error> Errors { get; }


        public LogoutResponse(IEnumerable<Error> errors, bool success = false, string message = null)
            : base(success, message)
        {
            Errors = errors;
        }


        public LogoutResponse(bool success = true, string message = null)
            : base(success, message)
        {
        }
    }
}
