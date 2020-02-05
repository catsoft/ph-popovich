﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using PhPopovich.Controllers.cms;

namespace PhPopovich.Models
{
    public class ProjectModel : Entity<ProjectModel>
    {
        [DisplayName("Заголовок")]
        [Show]
        public override string Title { get; set; }

        [DisplayName("Что за работа")]
        [Show(false)]
        [DataType(DataType.Html)]
        public string About { get; set; }

        [Show(false)]
        [DisplayName("Порядок")]
        public override int Position { get; set; }


        [Show(false, false, false, false)]
        public Guid? ImageModelId { get; set; }

        [Show(false, false)]
        [DisplayName("Изображение")]
        [Required]
        public ImageModel ImageModel { get; set; }
        
        
        [DisplayName("Изображения для галереи")]
        [OneTwoMany("ProjectModelGalleryId")]
        [Show(false, true, true, false)]
        [ShowTitle]
        public List<ImageModel> Images { get; set; }
    }
}