using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks; 

namespace VBlog.Services
{
    public class JsonFileService<T> where T : class
    {
        private readonly string _filePath;
        private readonly object _lock = new object(); 

        public JsonFileService(string filePath)
        {
            _filePath = filePath;
            
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
        }

        public List<T> GetAll()
        {
            lock (_lock) 
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
        }

        public void SaveAll(List<T> data)
        {
            lock (_lock) 
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented); 
                File.WriteAllText(_filePath, json);
            }
        }

        

        public void Add(T item)
        {
            var data = GetAll();
            data.Add(item);
            SaveAll(data);
        }

        public T? GetById(Func<T, bool> predicate)
        {
            return GetAll().FirstOrDefault(predicate);
        }

        
        public void Update(Func<T, bool> predicate, T updatedItem)
        {
            var data = GetAll();
            var existingItem = data.FirstOrDefault(predicate);
            if (existingItem != null)
            {
                

                
            }
            
        }

        public void Delete(Func<T, bool> predicate)
        {
            var data = GetAll();
            var itemToDelete = data.FirstOrDefault(predicate);
            if (itemToDelete != null)
            {
                data.Remove(itemToDelete);
            }
            SaveAll(data);
        }
    }
}