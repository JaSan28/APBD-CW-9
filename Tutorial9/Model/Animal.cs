using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model;

public class Animal
{
    public int IdAnimal { get; set; }
    [MaxLength(200)]
    public string Name { get; set; }
    public int Amount { get; set; }
}