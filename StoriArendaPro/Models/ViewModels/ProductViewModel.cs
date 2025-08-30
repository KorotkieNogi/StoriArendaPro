// Models/ViewModels/ProductViewModel.cs

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using StoriArendaPro.Attributes;
using StoriArendaPro.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StoriArendaPro.Models.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Наименование")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Артикул")]
        public string Sku { get; set; }

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Краткое описание")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Категория")]
        public int? CategoryId { get; set; }

        [Display(Name = "Тип оборудования")]
        public int? TypeProductId { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Цена за день (аренда)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal PricePerDay { get; set; }

        [Display(Name = "Залог")]
        [Range(0, double.MaxValue, ErrorMessage = "Залог не может быть отрицательным")]
        public decimal Deposit { get; set; }

        [Display(Name = "Минимальный срок аренды (дней)")]
        [Range(0.5, 365, ErrorMessage = "Минимальный срок от 0.5 до 365 дней")]
        public decimal MinRentalDays { get; set; } = 0.5m;

        [Display(Name = "Количество для аренды")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int QuantityForRent { get; set; }

        [Display(Name = "Количество для продажи")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int QuantityForSale { get; set; }

        [Display(Name = "Порог остатка")]
        [Range(1, int.MaxValue, ErrorMessage = "Порог должен быть не менее 1")]
        public int LowStockThreshold { get; set; } = 3;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Изображения")]
        [DataType(DataType.Upload)]
        public List<IFormFile>? Images { get; set; }

        public List<string>? ExistingImages { get; set; }
        public List<SelectListItem>? Categories { get; set; }
        public List<SelectListItem>? TypeProducts { get; set; }

        // Для отображения существующих изображений при редактировании
        public List<ProductImage>? ProductImages { get; set; }

        // Новое свойство для главного изображения
        public int? MainImageId { get; set; }
    }
}