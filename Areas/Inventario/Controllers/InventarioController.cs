using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaInventario.AccesoDatos.Data;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaInventario.Areas.Inventario.Controllers
{
	[Area("Inventario")]
	[Authorize(Roles = DS.Role_Admin+","+DS.Role_Inventario)]
	public class InventarioController : Controller
	{
		private readonly ApplicationDbContext _applicationDbContext;

		[BindProperty]
		public InventarioViewModel InventarioVM { get; set; }

		public InventarioController(ApplicationDbContext applicationDbContext)
		{
			_applicationDbContext = applicationDbContext;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult NuevoInventario(int? inventarioId)
		{
			InventarioVM = new InventarioViewModel();
			InventarioVM.BodegaLista = _applicationDbContext.Bodegas.ToList().Select(b => new SelectListItem
			{
				Text = b.Nombre,
				Value = b.Id.ToString()
			});
			InventarioVM.ProductoLista = _applicationDbContext.Productos.ToList().Select(p => new SelectListItem
			{
				Text = p.Descripcion,
				Value = p.Id.ToString()
			});

			InventarioVM.InventarioDetalles = new List<InventarioDetalle>();

			if(inventarioId != null)
			{
				InventarioVM.Inventario = _applicationDbContext.Inventarios.SingleOrDefault(i => i.Id == inventarioId);
				InventarioVM.InventarioDetalles = _applicationDbContext.InventarioDetalles
					.Include(p => p.Producto)
					.Include(m => m.Producto.Marca).Where(d => d.InventarioId == inventarioId)
					.ToList();
			}

			return View(InventarioVM);
		}

		[HttpPost]
		public IActionResult AgregarProductoPost(int producto, int cantidad, int inventarioId)
		{
			InventarioVM.Inventario.Id = inventarioId;
			if(InventarioVM.Inventario.Id == 0)
			{
				InventarioVM.Inventario.Estado = false;
				InventarioVM.Inventario.FechaInicial = DateTime.Now;

				var claimIdentidad = (ClaimsIdentity)User.Identity;
				var claim = claimIdentidad.FindFirst(ClaimTypes.NameIdentifier);

				InventarioVM.Inventario.UsuarioAplicacionId = claim.Value;

				_applicationDbContext.Inventarios.Add(InventarioVM.Inventario);
				_applicationDbContext.SaveChanges();
			}
			else
			{
				InventarioVM.Inventario = _applicationDbContext.Inventarios.SingleOrDefault(i => i.Id == inventarioId);

			}

			var bodegaProducto = _applicationDbContext.BodegaProductos.Include(b => b.Producto)
				.FirstOrDefault(b => b.ProductoId == producto && b.BodegaId == InventarioVM.Inventario.BodegaId);

			var detalle = _applicationDbContext.InventarioDetalles.Include(p => p.Producto)
				.FirstOrDefault(d => d.ProductoId == producto && d.InventarioId == InventarioVM.Inventario.Id);

			if(detalle == null)
			{
				InventarioVM.InventarioDetalle = new InventarioDetalle();
				InventarioVM.InventarioDetalle.ProductoId = producto;
				InventarioVM.InventarioDetalle.InventarioId = InventarioVM.Inventario.Id;
				if(bodegaProducto != null)
				{
					InventarioVM.InventarioDetalle.StockAnterior = bodegaProducto.Cantidad;
				}
				else
				{
					InventarioVM.InventarioDetalle.StockAnterior = 0;
				}
				InventarioVM.InventarioDetalle.Cantidad = cantidad;
				_applicationDbContext.InventarioDetalles.Add(InventarioVM.InventarioDetalle);
				_applicationDbContext.SaveChanges();
			}
			else
			{
				detalle.Cantidad += cantidad;
				_applicationDbContext.SaveChanges();
			}

			return RedirectToAction("NuevoInventario", new { inventarioId = InventarioVM.Inventario.Id });
		}

		public IActionResult Mas(int Id)
		{
			InventarioVM = new InventarioViewModel();
			var detalle = _applicationDbContext.InventarioDetalles.FirstOrDefault(d => d.Id == Id);
			InventarioVM.Inventario = _applicationDbContext.Inventarios.FirstOrDefault(i => i.Id == detalle.InventarioId);

			detalle.Cantidad += 1;
			_applicationDbContext.SaveChanges();

			return RedirectToAction("NuevoInventario", new { inventarioId = InventarioVM.Inventario.Id });
		}

		public IActionResult Menos(int Id)
		{
			InventarioVM = new InventarioViewModel();
			var detalle = _applicationDbContext.InventarioDetalles.FirstOrDefault(d => d.Id == Id);
			InventarioVM.Inventario = _applicationDbContext.Inventarios.FirstOrDefault(i => i.Id == detalle.InventarioId);

			if(detalle.Cantidad == 1)
			{
				_applicationDbContext.InventarioDetalles.Remove(detalle);
			}
			else
			{
				detalle.Cantidad -= 1;
			}

			_applicationDbContext.SaveChanges();
			return RedirectToAction("NuevoInventario", new { inventarioId = InventarioVM.Inventario.Id });
		}

		public IActionResult GenerarStock(int Id) 
		{
			var inventario = _applicationDbContext.Inventarios.FirstOrDefault(i => i.Id == Id);
			var detalleLista = _applicationDbContext.InventarioDetalles.Where(d => d.InventarioId == Id);

			foreach (var item in detalleLista)
			{
				var bodegaProducto = _applicationDbContext.BodegaProductos
					.Include(p => p.Producto).FirstOrDefault(b => b.ProductoId == item.ProductoId && b.BodegaId == inventario.BodegaId);

				if(bodegaProducto != null)
				{
					bodegaProducto.Cantidad += item.Cantidad;
				}
				else
				{
					bodegaProducto = new BodegaProducto();
					bodegaProducto.BodegaId = inventario.BodegaId;
					bodegaProducto.ProductoId = item.ProductoId;
					bodegaProducto.Cantidad = item.Cantidad;
					_applicationDbContext.Add(bodegaProducto);
				}
			}
			//Actualizar Cabecera del inventario
			inventario.Estado = true;
			inventario.FechaFinal = DateTime.Now;
			_applicationDbContext.SaveChanges();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Historial()
		{
			return View();
		}

		public IActionResult DetalleHistorial(int id)
		{
			var detalleHistorial = _applicationDbContext.InventarioDetalles
				.Include(p => p.Producto)
				.Include(m => m.Producto.Marca)
				.Where(d => d.InventarioId == id);

			return View(detalleHistorial);
		}


		#region API
		[HttpGet]
		public IActionResult ObtenerTodos()
		{
			var todos = _applicationDbContext.BodegaProductos.Include(b => b.Bodega).Include(p => p.Producto).ToList();
			return Json(new { data = todos });
		}

		[HttpGet]
		public IActionResult ObtenerHistorial()
		{
			var todos = _applicationDbContext.Inventarios
				.Include(b => b.Bodega)
				.Include(u => u.UsuarioAplicacion)
				.Where(i => i.Estado == true).ToList();

			return Json(new { data = todos });
		}
		#endregion
	}
}
