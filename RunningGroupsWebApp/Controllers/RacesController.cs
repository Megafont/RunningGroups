﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunningGroupsWebApp.Data;
using RunningGroupsWebApp.Interfaces;
using RunningGroupsWebApp.Models;
using RunningGroupsWebApp.Repositories;
using RunningGroupsWebApp.Interfaces;
using RunningGroupsWebApp.ViewModels;

namespace RunningGroupsWebApp.Controllers
{
    public class RacesController : Controller
    {
        private readonly IRacesRepository _RacesRepository;
        private readonly IPhotoService _PhotoService;
        private readonly IHttpContextAccessor _HttpContextAccessor;

        public RacesController(IRacesRepository racesRepository, IPhotoService photoService, IHttpContextAccessor httpContextAccessor)
        {
            _RacesRepository = racesRepository;
            _PhotoService = photoService;
            _HttpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var races = await _RacesRepository.GetAllAsync();
            return View(races);
        }

        public async Task<IActionResult> Detail(int id)
        {
            // NOTE: Use Include() sparingly as it is a much more expensive database operation.
            RaceModel race = await _RacesRepository.GetByIdAsync(id);
            return View(race);
        }

        public IActionResult Create()
        {
            var curUserId = _HttpContextAccessor.HttpContext.User.GetUserId(); // GetUserId() is an extension method we made in ClaimPrincipalExtensions.cs.

            var createRaceViewModel = new CreateRaceViewModel { AppUserId = curUserId };

            return View(createRaceViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRaceViewModel raceVM)
        {
            if (ModelState.IsValid)
            {
                var result = await _PhotoService.AddPhotoAsync(raceVM.Image);

                var race = new RaceModel
                {
                    Title = raceVM.Title,
                    Description = raceVM.Description,
                    Image = result.Url.ToString(),
                    AppUserId = raceVM.AppUserId,
                    Address = new AddressModel
                    {
                        Street = raceVM.Address.Street,
                        City = raceVM.Address.City,
                        State = raceVM.Address.State
                    },
                };

                _RacesRepository.Add(race);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Photo upload failed!");
            }

            return View(raceVM);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var race = await _RacesRepository.GetByIdAsync(id);
            if (race == null) return View("Error");
            var raceVM = new EditRaceViewModel
            {
                Title = race.Title,
                Description = race.Description,
                AddressId = race.AddressId,
                Address = race.Address,
                RaceCategory = race.RaceCategory,
                URL = race.Image
            };

            return View(raceVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditRaceViewModel raceVM)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Failed to edit race!");
                return View("Edit", raceVM);
            }

            var userRace = await _RacesRepository.GetByIdNoTrackingAsync(id);

            if (userRace != null)
            {
                try
                {
                    var fi = new FileInfo(userRace.Image);
                    var publicId = Path.GetFileNameWithoutExtension(fi.Name);
                    await _PhotoService.DeletePhotoAsync(publicId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Could not delete photo!");
                    return View(raceVM);
                }


                var photoResult = await _PhotoService.AddPhotoAsync(raceVM.Image);
                var race = new RaceModel()
                {
                    Id = id,
                    Title = raceVM.Title,
                    Description = raceVM.Description,
                    AppUserId = userRace.AppUserId,
                    AddressId = userRace.AddressId,
                    Address = new AddressModel
                    {
                        Id = userRace.AddressId,
                        Street = raceVM.Address.Street,
                        City = raceVM.Address.City,
                        State = raceVM.Address.State
                    },
                    RaceCategory = raceVM.RaceCategory,
                    Image = photoResult.Url.ToString(),
                };

                _RacesRepository.Update(race);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(raceVM);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var raceDetails = await _RacesRepository.GetByIdAsync(id);
            if (raceDetails == null) return View("Error");
            return View(raceDetails);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteClub(int id)
        {
            var clubDetails = await _RacesRepository.GetByIdAsync(id);
            if (clubDetails == null) return View("Error");

            // Delete the photo
            try
            {
                var fi = new FileInfo(clubDetails.Image);
                var publicId = Path.GetFileNameWithoutExtension(fi.Name);
                await _PhotoService.DeletePhotoAsync(publicId);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not delete photo!");
                return View(clubDetails);
            }

            // Delete the club
            _RacesRepository.Delete(clubDetails);
            return RedirectToAction(nameof(Index));
        }
    }
}
