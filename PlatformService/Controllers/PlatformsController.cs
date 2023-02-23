using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.Data;
using PlatformService.Dto;
using PlatformService.Models;
using PlatformService.AsyncDataServices;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    private readonly IPlatformRepo _repo;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _commandDataClient;
    private readonly IMessageBusClient _messageBusClient;

    public PlatformsController(IPlatformRepo repo, IMapper mapper, ICommandDataClient commandDataClient, IMessageBusClient messageBusClient)
    {
        _repo = repo;
        _mapper = mapper;
        _commandDataClient = commandDataClient;
        _messageBusClient = messageBusClient;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        var items = _repo.GetAllPlatforms();

        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(items));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var item = _repo.GetPlatformById(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<PlatformReadDto>(item));
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto platformCreateDto)
    {
        var platformModel = _mapper.Map<Platform>(platformCreateDto);
        _repo.CreatePlatform(platformModel);
        _repo.SaveChanges();

        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        // send sync message
        try
        {
            await _commandDataClient.SendPlatformToCommand(platformReadDto);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"--> Could not send sync: {ex.Message}");
        }

        // send async message
        try
        {
            var platformPublishedDto = _mapper.Map<PlatformPublishedDto>(platformReadDto);
            platformPublishedDto.Event = "Platform_Published";

            _messageBusClient.PublishNewPlatform(platformPublishedDto);

        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"--> Could not send async: {ex.Message}");
        }

        return CreatedAtRoute(nameof(GetPlatformById), new { Id = platformReadDto.Id }, platformReadDto);
    }

}