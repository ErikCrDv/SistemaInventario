using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.Utilidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaInventario.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = DS.Role_Admin)]
	public class UsuarioController : Controller
	{
		private readonly ApplicationDbContext _applicationDbContext;

		public UsuarioController(ApplicationDbContext applicationDbContext)
		{
			_applicationDbContext = applicationDbContext;
		}


		public IActionResult Index()
		{
			return View();
		}

		#region API
		[HttpGet]
		public IActionResult ObtenerTodos()
		{
			var usuarioLista = _applicationDbContext.UsuariosAplicacion.ToList();
			var userRole = _applicationDbContext.UserRoles.ToList();
			var roles = _applicationDbContext.Roles.ToList();

			foreach (var usuario in usuarioLista)
			{
				var roleId = userRole.FirstOrDefault(u => u.UserId == usuario.Id).RoleId;
				usuario.Role = roles.FirstOrDefault(u=> u.Id == roleId).Name;
			}

			return Json(new { data = usuarioLista });
		}


		[HttpPost]
		public IActionResult BloquearDesbloquear([FromBody] string id)
		{
			var usuario = _applicationDbContext.UsuariosAplicacion.FirstOrDefault(u => u.Id == id);
			if (usuario==null)
			{
				return Json(new { success= false, message = "Error de Usuario" });
			}
			if (usuario.LockoutEnd != null && usuario.LockoutEnd > DateTime.Now)
			{
				usuario.LockoutEnd = DateTime.Now;
			}else
			{
				usuario.LockoutEnd = DateTime.Now.AddYears(1000);
			}
			_applicationDbContext.SaveChanges();
			return Json(new { success = true,  message = "Operacion Exitosa" });
		}
		#endregion
	}
}
