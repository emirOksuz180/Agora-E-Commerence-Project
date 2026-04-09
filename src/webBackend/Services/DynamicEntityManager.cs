using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace webBackend.Services;

public class DynamicEntityManager
{
    private readonly AgoraDbContext _context;

    public DynamicEntityManager(AgoraDbContext context)
    {
        _context = context;
    }

    // GET: Edit/Details için veriyi dinamik çeker
   public async Task<object> GetEntityByIdAsync(string entityName, int id)
  {
      // 1. Adım: DbContext içindeki property'leri (tabloları) büyük/küçük harf bakmaksızın ara
      var prop = _context.GetType().GetProperties()
          .FirstOrDefault(p => p.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) || 
                              p.Name.Equals(entityName + "s", StringComparison.OrdinalIgnoreCase));

      if (prop == null)
      {
          // Tabloyu hiçbir şekilde bulamazsak hata fırlat ki nerede takıldığını loglardan görelim
          throw new Exception($"HATA: DbContext içerisinde '{entityName}' adına benzer bir tablo bulunamadı. " +
                              $"Lütfen DbContext içindeki DbSet ismini kontrol et!");
      }

      // 2. Adım: Tabloyu (DbSet) al
      var dbSet = prop.GetValue(_context) as IQueryable<object>;

      // 3. Adım: Veritabanında ID araması yap
      // Not: Eğer Id kolonun 'ProductId' gibi farklı bir isimdeyse burası null dönebilir.
      return await dbSet.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
  }

    // DELETE: Dinamik silme  
    public async Task<bool> DeleteEntityAsync(string entityName, int id)
    {
        var entity = await GetEntityByIdAsync(entityName, id);
        if (entity == null) return false;

        _context.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }


    

    public async Task<Dictionary<string, object>> GetLookupsAsync(string entityName)
    {
        var lookups = new Dictionary<string, object>();

        // 1. Entity'nin tipini bul (Örn: Product)
        var entityType = _context.Model.FindEntityType($"webBackend.Models.{entityName}");
        if (entityType == null) return lookups;

        // 2. Bu entity'deki tüm Foreign Key'leri (Dış anahtarları) tara
        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            var targetEntity = foreignKey.PrincipalEntityType; // Örn: Category
            var propertyName = targetEntity.ClrType.Name; // "Category"
            
            // Dinamik olarak hedef tabloyu sorgula
            var dbSetProp = _context.GetType().GetProperty(propertyName + "s"); // Genelde sonuna 's' gelir
            if (dbSetProp != null)
            {
                var dbSet = dbSetProp.GetValue(_context) as IQueryable<object>;
                var list = await dbSet!.ToListAsync();

                // Framework standardı: Her tabloda "Id" ve "Name" (veya "Title") olduğunu varsayıyoruz
                lookups.Add(propertyName + "List", list);
            }
        }

        return lookups;
    }



      

    public async Task<bool> SaveOrUpdateAsync(string entityName, object model)
    {
        // 1. Modelden Id'yi al (Edit mi Create mi anlamak için)
        var modelIdProp = model.GetType().GetProperty("ProductId") ?? model.GetType().GetProperty("Id");
        int modelId = (int)(modelIdProp?.GetValue(model) ?? 0);

        // 2. DB'deki gerçek tablo tipini bul
        var entityType = _context.Model.FindEntityType($"webBackend.Models.{entityName}")?.ClrType;
        if (entityType == null) return false;

        object? existingEntity;

        if (modelId > 0) 
        {
            // GÜNCELLEME (Edit)
            existingEntity = await _context.FindAsync(entityType, modelId);
            if (existingEntity == null) return false;

            // Modelden gelen değerleri DB'deki entity'ye otomatik aktar (Oto-Mapping)
            foreach (var modelProp in model.GetType().GetProperties())
            {
                var entityProp = entityType.GetProperty(modelProp.Name);
                // Sadece her iki tarafta da olan ve yazılabilir kolonları eşle
                if (entityProp != null && entityProp.CanWrite)
                {
                    var value = modelProp.GetValue(model);
                    entityProp.SetValue(existingEntity, value);
                }
            }
        }
        else 
        {
            // YENİ KAYIT (Create)
            existingEntity = Activator.CreateInstance(entityType);
            // Aynı mapping işlemi burada da geçerli
            foreach (var modelProp in model.GetType().GetProperties())
            {
                var entityProp = entityType.GetProperty(modelProp.Name);
                if (entityProp != null && entityProp.CanWrite)
                {
                    entityProp.SetValue(existingEntity, modelProp.GetValue(model));
                }
            }
            await _context.AddAsync(existingEntity!);
        }

        return await _context.SaveChangesAsync() > 0;
  }

  public async Task<List<object>> GetAllAsync(string entityName)
  {
      
      var entityType = _context.Model.FindEntityType($"webBackend.Models.{entityName}")?.ClrType;
      if (entityType == null) return new List<object>();

      
      var query = _context.GetType().GetMethods()
          .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
          .MakeGenericMethod(entityType)
          .Invoke(_context, null) as IQueryable;

      if (query == null) return new List<object>();

     
      return await query.Cast<object>().ToListAsync();
  }

  public async Task<bool> SaveFromDictionaryAsync(string entityName, Dictionary<string, string> data)
{
    var entityType = _context.Model.FindEntityType($"webBackend.Models.{entityName}")?.ClrType;
    if (entityType == null) return false;

    // Id'yi tespit et (Genelde 'Id' veya 'ProductId' gibi gelir)
    // Formdan gelen 'entityName + Id' veya direkt 'Id' anahtarını arıyoruz
    data.TryGetValue("Id", out var idStr);
    if (string.IsNullOrEmpty(idStr)) data.TryGetValue(entityName + "Id", out idStr);
    
    int.TryParse(idStr, out int id);

    object entity;
    if (id > 0)
    {
        // GÜNCELLEME
        entity = await _context.FindAsync(entityType, id);
        if (entity == null) return false;
    }
    else
    {
        // YENİ KAYIT
        entity = Activator.CreateInstance(entityType)!;
    }

    // Mapping: Dictionary içindeki verileri Entity property'leri ile eşleştir
    foreach (var item in data)
    {
        var prop = entityType.GetProperty(item.Key);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                // Tip dönüşümü (String'den int, bool, datetime vb.)
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var convertedValue = Convert.ChangeType(item.Value, targetType);
                prop.SetValue(entity, convertedValue);
            }
            catch { /* Tip uyuşmazlığı varsa pas geç veya logla */ }
        }
    }

      if (id == 0) await _context.AddAsync(entity);
      
      // Değişiklikleri kaydet ve sonucunu bir değişkene ata
      var saveResult = await _context.SaveChangesAsync();

      if (saveResult > 0)
      {
          // Tetikleyiciyi çalıştır
          await ExecuteBusinessTriggers(entityName, entity, id == 0 ? "Create" : "Update");
      }

      return saveResult > 0;
}


  private async Task ExecuteBusinessTriggers(string entityName, object entity, string actionType)
  {
      // TASK 11 & 12: Sipariş ve Stok Senkronizasyonu
      if (entityName == "Order" && actionType == "Update")
      {
          // Entity'yi gerçek tipine cast ediyoruz
          var order = entity as Order; 
          
          // Eğer sipariş "Onaylandı" (Status = 2 diyelim) statüsüne geçtiyse
          
      }
  }



  private async Task ProcessStockReduction(int orderId)
{
    // 1. Senin modelindeki 'OrderItems' tablosunu sorguluyoruz
    var orderItems = await _context.OrderItems
        .Where(oi => oi.OrderId == orderId)
        .ToListAsync();

    foreach (var item in orderItems)
    {
        // 2. Senin modelinde ProductId değil 'UrunId' kullanılmış
        var product = await _context.Products.FindAsync(item.UrunId);
        
        if (product != null)
        {
            // 3. Stoktan düşme işlemi
            // 'Miktar' senin modelindeki isim. 
            // product.StokAdedi kısmını Product.cs içindeki gerçek isimle kontrol et!
            product.Stock -= item.Miktar; 
        }
    }

    await _context.SaveChangesAsync();
}


}