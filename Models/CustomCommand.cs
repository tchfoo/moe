namespace TNTBot.Models
{
  public class CustomCommand
  {
    private string? description;

    public string Name { get; set; }
    public string Response { get; set; }
    public string? Description
    {
      get => description ?? "No description";
      set => description = value;
    }

    public CustomCommand(string name, string response, string? description)
    {
      Name = name;
      Response = response;
      Description = description;
    }
  }
}
