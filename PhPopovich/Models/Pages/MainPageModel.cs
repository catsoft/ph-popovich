﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EnglishApp.Controllers.cms;

namespace EnglishApp.Models.Pages
{
    [SingleObject]
    [Access(false, false, true, false)]
    public class MainPageModel : BasePage<MainPageModel>
    {
        [DisplayName("Основной текст")]
        [DataType(DataType.Html)]
        public string MainTitle { get; set; }

        [DisplayName("Под текст")]
        [DataType(DataType.Html)]
        public string MainSubTitle { get; set; }
    }
}