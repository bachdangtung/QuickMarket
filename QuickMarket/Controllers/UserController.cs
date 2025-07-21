using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services.Interfaces;
using System.Threading.Tasks;
using System.Security.Claims;
using BussinessLogic.DTOs.Users;
using System;

namespace QuickMarket.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: User/ManageUsers
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ManageUsers(string? searchQuery = null, int page = 1)
        {
            try
            {
                var pageSize = 20;
                var viewModel = await _userService.GetAllUsersAsync(searchQuery, page, pageSize);
                return View(viewModel);
            }
            catch (Exception)
            {
                // Log l'exception si nécessaire
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải dữ liệu người dùng.";
                return View(new UserManagementDto());
            }
        }
        
        // POST: User/UpdateStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int userId, string status)
        {
            var result = await _userService.UpdateUserStatusAsync(userId, status);
            
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message ?? "Failed to update user status" });
            }
            
            return Json(new { success = true });
        }
        
        // POST: User/UpdateRole
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int userId, int roleId)
        {
            var result = await _userService.UpdateUserRoleAsync(userId, roleId);
            
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message ?? "Failed to update user role" });
            }
            
            return Json(new { success = true });
        }
        
        // GET: User/Details/{id}
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    return NotFound();
                }
                
                return View(user);
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải thông tin người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }
        
        // GET: User/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    return NotFound();
                }
                
                return View(user);
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải thông tin người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }
        
        // POST: User/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, UserListDto model)
        {
            if (id != model.UserId)
            {
                return BadRequest();
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.UpdateUserRoleAsync(id, GetRoleIdFromName(model.RoleName));
                    await _userService.UpdateUserStatusAsync(id, model.Status);
                    
                    return RedirectToAction(nameof(Details), new { id = model.UserId });
                }
                catch (Exception)
                {
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi khi cập nhật thông tin người dùng.";
                }
            }
            
            return View(model);
        }
        
        // GET: User/Delete/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    return NotFound();
                }
                
                return View(user);
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải thông tin người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }
        
        // POST: User/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Nous changeons seulement le statut au lieu de supprimer
                var result = await _userService.UpdateUserStatusAsync(id, "Deleted");
                
                if (!result.Success)
                {
                    ViewBag.ErrorMessage = result.Message ?? "Đã xảy ra lỗi khi xóa người dùng.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
                
                return RedirectToAction(nameof(ManageUsers));
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi xóa người dùng.";
                return RedirectToAction(nameof(ManageUsers));
            }
        }
        
        // Helper pour convertir le nom du rôle en ID
        private int GetRoleIdFromName(string roleName)
        {
            return roleName switch
            {
                "Admin" => 1,
                "Manager" => 2,
                "Seller" => 3,
                "User" => 4,
                _ => 4 // Par défaut, le rôle User
            };
        }
        
        // GET: User/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // Si nous créons un nouvel utilisateur, vérifions d'abord les rôles disponibles
            var hasAdmin = await _userService.HasAdminUserAsync();
            
            ViewBag.HasAdmin = hasAdmin;
            
            return View(new CreateUserDto());
        }
        
        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateUserDto model)
        {
            // Vérifier s'il existe déjà un administrateur
            var hasAdmin = await _userService.HasAdminUserAsync();
            ViewBag.HasAdmin = hasAdmin;
            
            if (ModelState.IsValid)
            {
                // Si on essaie de créer un admin alors qu'il en existe déjà un
                if (model.RoleId == 1 && hasAdmin)
                {
                    ModelState.AddModelError("RoleId", "Le système ne peut avoir qu'un seul administrateur.");
                    return View(model);
                }
                
                var result = await _userService.CreateUserAsync(model);
                
                if (result.Success)
                {
                    return RedirectToAction(nameof(ManageUsers));
                }
                
                ModelState.AddModelError(string.Empty, result.Message ?? "Une erreur s'est produite lors de la création de l'utilisateur.");
            }
            
            return View(model);
        }
    }
}
