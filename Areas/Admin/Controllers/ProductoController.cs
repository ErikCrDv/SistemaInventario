using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaInventario.AccesoDatos.Repositorio.IRepositorio;
using SistemaInventario.Modelos;
using SistemaInventario.Modelos.ViewModels;
using SistemaInventario.Utilidades;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaInventario.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = DS.Role_Admin +","+ DS.Role_Inventario)]
	public class ProductoController : Controller
	{

		private readonly IUnidadTrabajo _unidadTrabajo;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public ProductoController(IUnidadTrabajo unidadTrabajo, IWebHostEnvironment webHostEnvironment)
		{
			_unidadTrabajo = unidadTrabajo;
			_webHostEnvironment = webHostEnvironment;
		}


		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Upsert(int? id)
		{
			ProductoVM productoVM = new ProductoVM()
			{
				Producto = new Producto(),
				CategoriaLista = _unidadTrabajo.Categoria.ObtenerTodos(c => c.Estado == true).Select(c => new SelectListItem
				{
					Text = c.Nombre,
					Value = c.Id.ToString()
				}),
				MarcaLista = _unidadTrabajo.Marca.ObtenerTodos(m => m.Estado == true).Select(m => new SelectListItem
				{
					Text = m.Nombre,
					Value = m.Id.ToString()
				}),
				PadreLista = _unidadTrabajo.Producto.ObtenerTodos().Select(p => new SelectListItem
				{
					Text = p.Descripcion,
					Value = p.Id.ToString()
				})
			};

			if(id == null)
			{
				return View(productoVM); //VISTA VACIA PARA CREAR
			}
			
			//VISTA CON LOS DATOS PARA ACTUALIZAR
			productoVM.Producto = _unidadTrabajo.Producto.Obtener(id.GetValueOrDefault());
			if(productoVM.Producto == null)
			{
				return NotFound();
			}

			return View(productoVM);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Upsert(ProductoVM productoVM)
		{
			if (ModelState.IsValid)
			{
				//CARGAR IMAGEN 
				string webRootPath = _webHostEnvironment.WebRootPath;
				var files = HttpContext.Request.Form.Files;
				if (files.Count > 0) // VERIFICAR SI LLEGA UNA IMAGEN 
				{
					string filename = Guid.NewGuid().ToString();
					var uploads = Path.Combine(webRootPath, @"imagenes\productos");
					var extension = Path.GetExtension(files[0].FileName);

					if (productoVM.Producto.ImageUrl != null)//EDITAR CAMPO DE LA IMAGEN (eliminar/insertar)
					{
						var imagenPath = Path.Combine(webRootPath, productoVM.Producto.ImageUrl.TrimStart('\\'));
						if (System.IO.File.Exists(imagenPath))
						{
							System.IO.File.Delete(imagenPath);//ELIMINAR IMAGEN EXISTENTE
						}
					}

					using (var filesStreams = new FileStream(Path.Combine(uploads, filename + extension), FileMode.Create))
					{
						files[0].CopyTo(filesStreams);
					}
					productoVM.Producto.ImageUrl = @"\imagenes\productos\" + filename + extension;
				}
				else
				{
					//Si no se actualiza la imagen se vulve a cargar la que ya estaba registrada
					if(productoVM.Producto.Id != 0)
					{
						Producto productoDB = _unidadTrabajo.Producto.Obtener(productoVM.Producto.Id);
						productoVM.Producto.ImageUrl = productoDB.ImageUrl;
					}


				}

				if(productoVM.Producto.Id == 0)
				{
					_unidadTrabajo.Producto.Agregar(productoVM.Producto);
				}
				else
				{
					_unidadTrabajo.Producto.Actualizar(productoVM.Producto);
				}
				_unidadTrabajo.Guardar();
				return RedirectToAction(nameof(Index));
			}
			else
			{
				productoVM.CategoriaLista = _unidadTrabajo.Categoria.ObtenerTodos().Select(c => new SelectListItem
				{
					Text = c.Nombre,
					Value = c.Id.ToString()
				});
				productoVM.MarcaLista = _unidadTrabajo.Marca.ObtenerTodos().Select(m => new SelectListItem
				{
					Text = m.Nombre,
					Value = m.Id.ToString()
				});
				productoVM.PadreLista = _unidadTrabajo.Producto.ObtenerTodos().Select(p => new SelectListItem
				{
					Text = p.Descripcion,
					Value = p.Id.ToString()
				});

				if (productoVM.Producto.Id != 0)
				{
					productoVM.Producto = _unidadTrabajo.Producto.Obtener(productoVM.Producto.Id);
				}
			}
			return View(productoVM.Producto);
		}




		#region API
		[HttpGet]
		public IActionResult ObtenerTodos()
		{
			var todos = _unidadTrabajo.Producto.ObtenerTodos(incluirPropiedades: "Categoria,Marca");
			return Json(new { data = todos });
		}

		[HttpDelete]
		public IActionResult Delete(int id)
		{
			var productoDb = _unidadTrabajo.Producto.Obtener(id);
			if(productoDb == null)
			{
				return Json(new { success = false, message = "Error al borrar" } );
			}

			//Eliminar Imagen
			string webRootPath = _webHostEnvironment.WebRootPath;
			var imagenPath = Path.Combine(webRootPath, productoDb.ImageUrl.TrimStart('\\'));
			if (System.IO.File.Exists(imagenPath))
			{
				System.IO.File.Delete(imagenPath);
			}

			_unidadTrabajo.Producto.Remover(productoDb);
			_unidadTrabajo.Guardar();
			return Json( new { success = true, message = "Producto eliminada exitosamente" } );
		}
		#endregion
	}
}
