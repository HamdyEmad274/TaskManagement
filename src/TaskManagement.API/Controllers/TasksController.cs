using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Application.Tasks.Commands.DeleteTask;
using TaskManagement.Application.Tasks.Commands.UpdateTask;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Application.Tasks.Queries.GetAllTasksByUser;
using TaskManagement.Application.Tasks.Queries.GetTaskById;

namespace TaskManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ISender _sender;

        public TasksController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cmd = new CreateTaskCommand(request.Title, request.Description, request.Priority, Guid.Parse(userId!));
            var result = await _sender.Send(cmd, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cmd = new GetTaskByIdQuery(id, Guid.Parse(userId!));
            var result = await _sender.Send(cmd, cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllByUser()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var cmd = new GetAllTasksByUserQuery(userId);
            var result = await _sender.Send(cmd);
            if (result.IsFailure)
                return NotFound(result.Error);
            return Ok(result.Value);
        }
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cmd = new UpdateTaskStatusCommand(id, request.Status, Guid.Parse(userId!));
            var result = await _sender.Send(cmd, cancellationToken);
            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }
            return Ok(result.Value);
        }
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cmd = new DeleteTaskCommand(id, Guid.Parse(userId!));
            var result = await _sender.Send(cmd, cancellationToken);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return NoContent();
        }
    }
}
