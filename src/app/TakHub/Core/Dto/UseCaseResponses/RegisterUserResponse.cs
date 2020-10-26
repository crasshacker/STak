using System.Collections.Generic;
using STak.TakHub.Core.Interfaces;

namespace STak.TakHub.Core.Dto.UseCaseResponses
{
    public class RegisterUserResponse : UseCaseResponseMessage 
    {
        public string              Id     { get; }
        public IEnumerable<string> Errors {  get; }


        public RegisterUserResponse(IEnumerable<string> errors, bool success = false, string message = null)
            : base(success, message)
        {
            Errors = errors;
        }


        public RegisterUserResponse(string id, bool success = false, string message = null)
            : base(success, message)
        {
            Id = id;
        }
    }
}
