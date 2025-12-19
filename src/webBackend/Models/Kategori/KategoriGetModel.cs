namespace webBackend.Models;



public class KategoriGetModel
{
  public int Id { get; set; }

  public string Name { get; set; } = null!;

  public string Url { get; set; } = null!;

  public int ProductCount { get; set; }
}