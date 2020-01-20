﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnglishApp.Controllers.cms;

namespace EnglishApp.Models
{
    [Access(false, false, false, false)]
    public class ImageModel : Entity<ImageModel>
    {
        [DataType(DataType.ImageUrl)]
        [DisplayName("Ссылка на диске")]
        public string Url { get; set; }

        public string ImageType { get; set; }

        public string Extension { get; set; }
    }
}