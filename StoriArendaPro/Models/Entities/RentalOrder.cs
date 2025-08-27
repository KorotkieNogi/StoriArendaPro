using System;
using System.Collections.Generic;

namespace StoriArendaPro.Models.Entities;

public partial class RentalOrder
{
    public int RentalOrderId { get; set; }  //номер заказа

    public int? UserId { get; set; }   //Кто арендует


    public DateTime StartDate { get; set; }  //Дата начала аренды

    public DateTime EndDate { get; set; } //Дата окончания

    public decimal TotalAmount { get; set; } //Общая сумма

    public string? Status { get; set; } //Статус заказа  'ожидает', 'подтверждено', 'активно', 'завершено', 'отменено'

    public string? PaymentStatus { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RentalOrderItem> RentalOrderItems { get; set; } = new List<RentalOrderItem>();

    public virtual User? User { get; set; }
}
