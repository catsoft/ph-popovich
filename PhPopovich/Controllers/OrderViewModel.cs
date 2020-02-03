﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PhPopovich.Controllers
{
    public class OrderViewModel
    { 
        [Required(ErrorMessage = "Вам нужно указать имя и фамилию")]
        [DisplayName("Имя фамилия")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Вам нужно указать контактные данные")]
        [DisplayName("Телефон или почта")]
        public string EmailOrPhone { get; set; }
        
        [Required(ErrorMessage = "Вам нужно указать желаему дату")]
        [DataType(DataType.Date)]
        [JsonConverter(typeof(MyDateTimeConverter))]
        [DisplayName("Желаемая дата")]
        public DateTime DesireDate { get; set; } = DateTime.Now.AddDays(1);
        
        [Required(ErrorMessage = "Опишите чтобы выхотели")]
        [DisplayName("Чтобы вы хотели")]
        public string About { get; set; }
    }
}