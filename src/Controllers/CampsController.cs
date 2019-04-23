using System;
using System.Threading.Tasks;
using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Internal;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoreCodeCamp.Controllers
{
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	[ApiVersion("1.1")]
	[ApiController]
	public class CampsController : ControllerBase
	{
		private readonly ICampRepository _repository;
		private readonly IMapper _mapper;
		private readonly LinkGenerator _linkGenerator;

		public CampsController(ICampRepository repository,IMapper mapper, LinkGenerator linkGenerator)
		{
			_repository = repository;
			_mapper = mapper;
			_linkGenerator = linkGenerator;
		}

		[HttpGet]
		public async Task<ActionResult<CampModel[]>> Get(bool includeTalks = false)
		{
			try
			{
				var results = await _repository.GetAllCampsAsync(includeTalks);
				return _mapper.Map<CampModel[]>(results);
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}
		}

		[HttpGet("{moniker}")]
		[MapToApiVersion("1.0")]
		public async Task<ActionResult<CampModel>> Get(string moniker)
		{
			try
			{
				var result = await _repository.GetCampAsync(moniker);
				if (result == null)
					return NotFound();

				return _mapper.Map<CampModel>(result);
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}
		}

		[HttpGet("{moniker}")]
		[MapToApiVersion("1.1")]
		public async Task<ActionResult<CampModel>> Get11(string moniker)
		{
			try
			{
				var result = await _repository.GetCampAsync(moniker, true);
				if (result == null)
					return NotFound();

				return _mapper.Map<CampModel>(result);
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}
		}

		[HttpGet("search")]
		public async Task<ActionResult<CampModel[]>> SearchByDate(DateTime theDate, bool includeTalks = false)
		{
			try
			{
				var results = await _repository.GetAllCampsByEventDate(theDate, includeTalks);

				if (!results.Any())
					return NotFound();

				return _mapper.Map<CampModel[]>(results);

			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}
		}

		public async Task<ActionResult<CampModel>> Post(CampModel model)
		{
			try
			{
				var existingCamp = await _repository.GetCampAsync(model.Moniker);
				if (existingCamp != null)
				{
					return BadRequest("Moniker in use");
				}

				var location = _linkGenerator.GetPathByAction("Get", "Camps", new {moniker = model.Moniker});
				if (string.IsNullOrWhiteSpace(location))
				{
					return BadRequest("Could not use current moniker");
				}

				var camp = _mapper.Map<Camp>(model);
				_repository.Add(camp);
				if (await _repository.SaveChangesAsync())
				{
					return Created(location, _mapper.Map<CampModel>(camp));
				}
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}

			return BadRequest();
		}

		[HttpPut("{moniker}")]
		public async Task<ActionResult<CampModel>> Put(string moniker, CampModel model)
		{
			try
			{
				var oldCamp = await _repository.GetCampAsync(moniker);
				if (oldCamp == null)
					return NotFound($"Could not find camp with moniker of {moniker}");

				_mapper.Map(model, oldCamp);

				if (await _repository.SaveChangesAsync())
				{
					return _mapper.Map<CampModel>(oldCamp);
				}
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}

			return BadRequest();
		}

		[HttpDelete("{moniker}")]
		public async Task<IActionResult> Delete(string moniker)
		{
			try
			{
				var oldCamp = await _repository.GetCampAsync(moniker);
				if (oldCamp == null)
					return NotFound($"Could not find camp with moniker of {moniker}");

				_repository.Delete(oldCamp);

				if (await _repository.SaveChangesAsync())
				{
					return Ok();
				}
			}
			catch (Exception e)
			{
				return this.StatusCode(StatusCodes.Status500InternalServerError, "Database error");
			}

			return BadRequest();
		}
	}
}