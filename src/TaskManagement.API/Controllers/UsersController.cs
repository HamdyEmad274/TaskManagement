using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Users.Commands.DeleteUser;
using TaskManagement.Application.Users.Commands.LoginUser;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Application.Users.Queries.GetAllUsers;
using TaskManagement.Application.Users.Queries.GetUserById;

namespace TaskManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ISender _sender;

        public UsersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            var command = new RegisterUserCommand(request.UserName, request.Email, request.Password);
            var result = await _sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Contains("already exists"))
                {
                     return Conflict(result.Error);
                }
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var command = new LoginUserCommand(request.Email, request.Password);
            var result = await _sender.Send(command);
            if (result.IsFailure)
            {
                return Unauthorized(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var cmd = new GetUserByIdQuery(id);
            var result = await _sender.Send(cmd);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var cmd = new GetAllUsersQuery();
            var result = await _sender.Send(cmd);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpDelete("{id:guid}")] 
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteUserCommand(id);
            var result = await _sender.Send(command);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return NoContent();
        }

    }
}
