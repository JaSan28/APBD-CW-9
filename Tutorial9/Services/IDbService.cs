using Tutorial9.Model;

namespace Tutorial9.Services
{
    public interface IDbService
    {
        Task DoSomethingAsync();
        Task ProcedureAsync();
        
        Task<int> AddProductToWarehouseAsync(ProductWarehouseRequest request);
        Task<int> AddProductToWarehouseWithProcedureAsync(ProductWarehouseRequest request);
    }
}