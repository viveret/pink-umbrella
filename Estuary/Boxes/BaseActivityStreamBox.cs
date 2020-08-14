using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Estuary.Core;
using Estuary.Objects;
using Estuary.Streams;
using Estuary.Streams.Json;
using Estuary.Util;
using Tides.Models.Public;

namespace Estuary.Services.Boxes
{
    public class BaseActivityStreamBox : IActivityStreamBox
    {
        public static readonly Regex IndexRegex = new Regex(@"\w{1,20}");

        public CustomJsonSerializer _serializer { get; } = new CustomJsonSerializer();
        
        public List<BaseObjectStreamWriter> Writers { get; } = new List<BaseObjectStreamWriter>();

        public ActivityStreamFilter filter { get; }

        public IActivityStreamRepository ctx { get; }

        public BaseActivityStreamBox(ActivityStreamFilter filter, IActivityStreamRepository ctx)
        {
            this.filter = filter;
            this.ctx = ctx;
            //Writers.Add(new ObjectStreamWriter(File.Open(PathOf(null), FileMode.Append, FileAccess.Write, FileShare.Read), _serializer, ctx));
            Writers.Add(new ObjectIdStreamWriter(File.Open(PathOf(null), FileMode.Append, FileAccess.Write, FileShare.Read), ctx));
        }

        public async Task<CollectionObject> Get(ActivityStreamFilter filter)
        {
            var ret = new List<Task<BaseObject>>();
            var ctxs = new List<ActivityDeliveryContext>();
            var pipe = this.ctx.GetPipe();
            using (var reader = await OpenReader(filter))
            {
                while (true)
                {
                    var item = await reader.Read();
                    if (item == null)
                    {
                        break;
                    }
                    else if (item is Error err)
                    {
                        ctxs.Add(null);
                        ret.Add(Task.FromResult(item));
                    }
                    else if (item is ActivityObject activity)
                    {
                        if (filter.IsMatch(activity))
                        {
                            var ctx = new ActivityDeliveryContext
                            {
                                IsReading = true,
                                context = this.ctx, box = this, item = activity, Filter = filter
                            };
                            ctxs.Add(ctx);
                            ret.Add(pipe.Pipe(ctx));
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid type {item.type}");
                    }
                }
            }
            var pipedRet = await Task.WhenAll(ret);
            return pipedRet.Select((res, i) => res ?? ctxs[i].item).Where(x => x != null).ToCollection();
        }

        public async Task<BaseObject> Write(BaseObject item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            else if (string.IsNullOrWhiteSpace(item.id))
            {
                throw new ArgumentNullException("item.id");
            }

            var storePath = PathOf(item.PublicId);
            if (!File.Exists(storePath))
            {
                using (var storeWriter = new ObjectStreamWriter(File.Open(storePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), _serializer, ctx))
                {
                    await storeWriter.Write(item);
                }

                foreach (var writer in Writers)
                {
                    await writer.Write(item);
                }
            }
            return item;
        }

        protected virtual Task<BaseObjectStreamReader> OpenReader(ActivityStreamFilter filter)
        {
            BaseObjectStreamReader ret;
            var path = PathOf(filter.publicId);
            if (System.IO.File.Exists(path))
            {
                switch (System.IO.Path.GetExtension(path))
                {
                    case ".index":
                    {
                        var s = System.IO.File.OpenRead(path);
                        if (filter.reverse)
                        {
                            ret = new ReverseObjectIdStreamReader(s, id =>
                            {
                                var p = PathOf(id);
                                if (System.IO.File.Exists(p))
                                {
                                    return System.IO.File.OpenRead(p);
                                }
                                else
                                {
                                    return null;
                                }
                            }, _serializer);
                        }
                        else
                        {
                            ret = new ObjectIdStreamReader(s, id => System.IO.File.OpenRead(PathOf(id)), _serializer);
                        }
                    }
                    break;
                    case ".json":
                    ret = new ObjectStreamReader(System.IO.File.OpenRead(path), _serializer);
                    break;
                    default:
                    throw new Exception($"Invalid activity stream type: {path}");
                }
            }
            else
            {
                ret = new EmptyObjectStreamReader();
            }
            return Task.FromResult(ret);
        }

        protected virtual string PathOf(PublicId id)
        {
            var path = $"Upload/activitystreams";
            if (filter.peerId.HasValue)
            {
                path = $"{path}/peers/{filter.peerId.Value}";
            }

            if (filter.userId.HasValue)
            {
                path = $"{path}/users/{filter.userId.Value}";
            }
            
            if (string.IsNullOrWhiteSpace(filter.index))
            {
                throw new ArgumentNullException("filter.index");
            }
            else if (!IndexRegex.IsMatch(filter.index))
            {
                throw new Exception("Index contains invalid characters or is an invalid length");
            }
            
            if (id != null && id.IsGuid)
            {
                path = $"{path}/activities/{id.AsGuid()}.json";
            }
            else
            {
                path = $"{path}/indices/{filter.index}.index";
            }

            var parentFolder = System.IO.Directory.GetParent(path);
            if (!parentFolder.Exists)
            {
                parentFolder.Create();
            }

            return path;
        }
    }
}