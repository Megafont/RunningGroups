﻿using System.ComponentModel.DataAnnotations;

namespace RunningGroupsWebApp.Models
{
    public class AddressModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Street { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }

    }
}
