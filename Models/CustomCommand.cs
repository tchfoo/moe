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
    public bool Delete { get; set; }

    public CustomCommand(string name, string response, string? description, bool delete)
    {
      Name = name;
      Response = response;
      Description = description;
      Delete = delete;
    }
  }
}
