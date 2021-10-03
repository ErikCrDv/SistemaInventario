using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaInventario.Areas.Inventario.Controllers
{
	[Area("Inventario")]
	public class HomeController : Controller
	{

		private readonly ILogger<HomeController> _logger;
		private readonly IUnidadTrabajo _unidadTrabajo;


		public HomeController(ILogger<HomeController> logger, IUnidadTrabajo unidadTrabajo)
		{
			_logger = logger;
			_unidadTrabajo = unidadTrabajo;
		}

		public IActionResult Index()
		{
			IEnumerable<Producto> productoLista = _unidadTrabajo.Producto.ObtenerTodos(incluirPropiedades: "Categoria,Marca");
			return View(productoLista);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
