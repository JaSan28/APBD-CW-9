using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouseAsync(ProductWarehouseRequest request)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await connection.OpenAsync();
        
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        
        try
        {
            // 1. Weryfikacja czy produkt i magazyn istnieją
            var productExists = await CheckIfExists(connection, transaction, "Product", request.IdProduct);
            var warehouseExists = await CheckIfExists(connection, transaction, "Warehouse", request.IdWarehouse);

            if (!productExists || !warehouseExists || request.Amount <= 0)
            {
                throw new Exception("Invalid product, warehouse or amount");
            }

            // 2. Wyszukaj odpowiednie zamówienie
            var orderId = await FindMatchingOrder(connection, transaction, request);
            if (orderId == null)
            {
                throw new Exception("No matching order found");
            }

            // 3. Sprawdź czy zrealizowano zamówienie
            var orderFulfilled = await CheckIfOrderFulfilled(connection, transaction, orderId.Value);
            if (orderFulfilled)
            {
                throw new Exception("Order already fulfilled");
            }

            // 4. Aktualizacja zamówienia
            await UpdateOrderFulfilledAt(connection, transaction, orderId.Value, request.CreatedAt);

            // 5. Wstawienie rekordu
            var newId = await InsertProductWarehouse(connection, transaction, request, orderId.Value);

            await transaction.CommitAsync();
            return newId;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> AddProductToWarehouseWithProcedureAsync(ProductWarehouseRequest request)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand("AddProductToWarehouse", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        
        return Convert.ToInt32(result);
    }
    
    private async Task<bool> CheckIfExists(SqlConnection connection, SqlTransaction transaction, string table, int id)
    {
        var query = $"SELECT 1 FROM {table} WHERE Id{table} = @Id";
        await using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@Id", id);
        
        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<int?> FindMatchingOrder(SqlConnection connection, SqlTransaction transaction, ProductWarehouseRequest request)
    {
        const string query = @"
            SELECT TOP 1 IdOrder FROM [Order] 
            WHERE IdProduct = @IdProduct AND Amount = @Amount 
            AND CreatedAt < @CreatedAt";
            
        await using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        var result = await command.ExecuteScalarAsync();
        return result != null ? (int)result : null;
    }

    private async Task<bool> CheckIfOrderFulfilled(SqlConnection connection, SqlTransaction transaction, int orderId)
    {
        const string query = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        await using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@IdOrder", orderId);

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    private async Task UpdateOrderFulfilledAt(SqlConnection connection, SqlTransaction transaction, int orderId, DateTime fulfilledAt)
    {
        const string query = "UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
        await using var command = new SqlCommand(query, connection, transaction);
        command.Parameters.AddWithValue("@IdOrder", orderId);
        command.Parameters.AddWithValue("@FulfilledAt", fulfilledAt);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertProductWarehouse(SqlConnection connection, SqlTransaction transaction, ProductWarehouseRequest request, int orderId)
    {
        const string priceQuery = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        await using var priceCommand = new SqlCommand(priceQuery, connection, transaction);
        priceCommand.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        
        var price = (decimal)await priceCommand.ExecuteScalarAsync();
        var totalPrice = price * request.Amount;

        const string insertQuery = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int);";
            
        await using var insertCommand = new SqlCommand(insertQuery, connection, transaction);
        insertCommand.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        insertCommand.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        insertCommand.Parameters.AddWithValue("@IdOrder", orderId);
        insertCommand.Parameters.AddWithValue("@Amount", request.Amount);
        insertCommand.Parameters.AddWithValue("@Price", totalPrice);
        insertCommand.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        return (int)await insertCommand.ExecuteScalarAsync();
    }

   
    public async Task DoSomethingAsync() { /* ... */ }
    public async Task ProcedureAsync() { /* ... */ }
}