using Moq;
using STak.TakHub.Core.Dto.GatewayResponses.Repositories;
using STak.TakHub.Core.Dto.UseCaseRequests;
using STak.TakHub.Core.Dto.UseCaseResponses;
using STak.TakHub.Core.Interfaces;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.UseCases;
using Xunit;

namespace STak.TakHub.Core.UnitTests.UseCases
{
    public class RegisterUserUseCaseUnitTests
    {
        [Fact]
        public async void Handle_GivenValidRegistrationDetails_ShouldSucceed()
        {
            // arrange

            // 1. We need to store the user data somehow
            var mockUserRepository = new Mock<IUserRepository>();
            mockUserRepository
              .Setup(repo => repo.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new CreateUserResponse("", true));

            // 2. The use case and star of this test
            var useCase = new RegisterUserUseCase(mockUserRepository.Object);

            // 3. The output port is the mechanism to pass response data from the use case to a Presenter 
            // for final preparation to deliver back to the UI/web page/api response etc.
            var mockOutputPort = new Mock<IOutputPort<RegisterUserResponse>>();
            mockOutputPort.Setup(outputPort => outputPort.Handle(It.IsAny<RegisterUserResponse>()));

            // act

            // 4. We need a request model to carry data into the use case from the upper layer (UI, Controller etc.)
            var response = await useCase.Handle(new RegisterUserRequest("firstName", "lastName", "me@domain.com", "userName", "password"), mockOutputPort.Object);

            // assert
            Assert.True(response);
        }
    }
}
