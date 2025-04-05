using System.ComponentModel.DataAnnotations;

namespace DogTracker.Enums;

public enum CategoryEnum
{
    [Display(Name = "Croquettes")]
    Croquettes,
    [Display(Name = "Friandises")]
    Friandises,
    [Display(Name = "Vétérinaire")]
    Vétérinaire,
    [Display(Name = "Accessoires")]
    Accessoires,
    [Display(Name = "Jouets")]
    Jouets,
    [Display(Name = "Santé")]
    Santé,
    [Display(Name = "Autre")]
    Autre
}