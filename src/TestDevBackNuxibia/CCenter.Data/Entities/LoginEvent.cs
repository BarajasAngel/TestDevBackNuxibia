namespace CCenter.Data.Entities;

public sealed class LoginEvent
{
    public long Id { get; set; }          
    public int UserId { get; set; }       
    public int Extension { get; set; }    
    public byte TipoMov { get; set; }     
    public DateTime Fecha { get; set; }   
}
