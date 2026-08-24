using System.Collections.Generic;
using System.Collections;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Realso.Auth.Binders
{
  public class HashtableBinder : IModelBinder
  {
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
      if (bindingContext == null)
      {
        throw new ArgumentNullException(nameof(bindingContext));
      }

      var modelName = bindingContext.ModelName;

      // Try to fetch the value of the argument by name
      var valueProviderResult =
          bindingContext.ValueProvider.GetValue(modelName);

      if (valueProviderResult == ValueProviderResult.None)
      {
        return Task.CompletedTask;
      }

      bindingContext.ModelState.SetModelValue(modelName,
          valueProviderResult);

      var value = valueProviderResult.FirstValue;

      // Check if the argument value is null or empty
      if (string.IsNullOrEmpty(value))
      {
        return Task.CompletedTask;
      };
      //JsonSerializerSettings settings = new JsonSerializerSettings();
      //settings.MaxDepth = 2;
      //Hashtable ret = JsonConvert.DeserializeObject( value,typeof(Hashtable), settings) as Hashtable;
      Hashtable ret = JsonConvert.DeserializeObject<Hashtable>(value);
      JToken jt = JsonConvert.DeserializeObject<JToken>(value);

      bindingContext.Result = ModelBindingResult.Success(converToHash(ret));
      return Task.CompletedTask;
    }

    private object converToHash(Hashtable jt)
    {
      if (jt == null)
        return null;
      Hashtable ht = new Hashtable();
      foreach (DictionaryEntry e in jt)
      {
        var jobj = e.Value as JToken;
        if (jobj != null)
        {
          if (jobj.Type == JTokenType.Array)
          {
            ArrayList array = jobj.ToObject<ArrayList>();
            ht[e.Key] = array.ToArray();
          }
          else
          {
            ht[e.Key] = converToHash(jobj.ToObject<Hashtable>());
          }
        }
        else
        {
          ht[e.Key] = e.Value;
        }
      }
      return ht;
    }

    /// <summary>
    /// dictionary 转为 hashtable
    /// </summary>
    /// <param name="d">dictionary</param>
    /// <returns>hashtable</returns>
    private object converToHash(JToken jt)
    {
      if (jt == null)
        return null;
      Hashtable ht = new Hashtable();

      JsonReader reader = jt.CreateReader();

      //JProperty.Load();
      while (reader.Read())
      {
        if (reader.Value != null)
        {


          JProperty jp = jt[reader.Value].ToObject<JProperty>();
          if (jt.HasValues && jt[reader.Value] is JToken)
          {
            ht[reader.Value] = converToHash(jt[reader.Value]);
          }
          else
          {
            if (ht.Keys.Count == 0)
              return reader.Value;
          }
        }
      }

      return ht;
    }

  }
}
