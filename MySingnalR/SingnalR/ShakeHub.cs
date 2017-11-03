using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MySingnalR.SingnalR
{

    [HubName("shakeHub")]
    public class ShakeHub : Hub
    {

        private static ConcurrentDictionary<string, List<SUser>> _users = new ConcurrentDictionary<string, List<SUser>>();

        /// <summary>
        /// 设置当前应用程序指定CacheKey的Cache值  相对过期（滑动过期）
        /// </summary>
        /// <param name="CacheKey">CacheKey名字</param>  
        /// <param name="objObject">CacheKey值</param> 
        /// <param name="seconds">设置过期时间大小 秒为单位</param>
        public static void SetCacheN(string CacheKey, object objObject, long seconds)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            objCache.Insert(CacheKey, objObject, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
                TimeSpan.FromSeconds(seconds));
        }

        /// <summary>  
        /// 获取当前应用程序指定CacheKey的Cache值  
        /// </summary>  
        /// <param name="CacheKey">CacheKey名字</param>  
        /// <returns>对应的CacheKey值</returns>  
        public static object GetCache(string CacheKey)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            return objCache.Get(CacheKey);
        }

        #region Other


        /// <summary>
        /// 创建链接
        /// </summary>
        /// <returns></returns>
        public async override Task OnConnected()
        {
            await base.OnConnected();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            foreach (var item in _users.Values)
            {
                if (item.Find(i => i.ConnectionId == Context.ConnectionId) != null)
                    item.Remove(item.Find(i => i.ConnectionId == Context.ConnectionId));
            }
            return base.OnDisconnected(stopCalled);
        }

        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns></returns>
        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion

        #region PC Side

        /// <summary>
        /// 返回在线人数
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> PcInit(int id)
        {
            if (id <= 0) throw new HubException("无效参数");
            SetCacheN("PC" + id, Context.ConnectionId, 3600);
            await Clients.Group("PCId" + id).Reset();//推送移动端重置
            List<SUser> lstUser = new List<SUser>();
            _users.TryGetValue("Mobile" + id, out lstUser);
            return lstUser == null ? 0 : lstUser.Count();
        }

        #endregion

        #region Mobile

        /// <summary>
        /// 初始化客户端 处理用户信息 以及分组
        /// </summary>
        /// <param name="id">抖一抖游戏序号</param>
        /// <returns></returns>
        public async Task<bool> MobileInit(int id)
        {
            if (id <= 0) throw new HubException("无效参数");
            await Groups.Add(Context.ConnectionId, "PCId" + id);//移动用户分组
            var maxId = 0;
            Random rd = new Random();
            string defaultName = "潘" + rd.Next();
            SUser su = null;
            List<SUser> lstUser = new List<SUser>();
            if (_users.TryGetValue("Mobile" + id, out lstUser))
            {
                var user = lstUser.Find(s => s.ConnectionId == Context.ConnectionId);
                if (user == null)
                {
                    su = new SUser() { ConnectionId = Context.ConnectionId, Count = 0, Id = maxId, Img = "http://h.hiphotos.baidu.com/image/pic/item/4ec2d5628535e5dd2820232370c6a7efce1b623a.jpg", Name = defaultName };
                    lstUser.Add(su);
                }
            }
            else
            {
                if (lstUser == null) lstUser = new List<SUser>();
                su = new SUser() { ConnectionId = Context.ConnectionId, Count = 0, Id = maxId, Img = "http://h.hiphotos.baidu.com/image/pic/item/4ec2d5628535e5dd2820232370c6a7efce1b623a.jpg", Name = defaultName };
                lstUser.Add(su);
            }

            _users.GetOrAdd("Mobile" + id, lstUser);
            var pcid = GetCache("PC" + id) as string;
            if (!string.IsNullOrEmpty(pcid)) await Clients.Client(pcid).PutUser(su, lstUser.Count); //推送给PC 展示参与进来的移动端信息 和当前在线总数量
            return true;
        }

        /// <summary>
        /// 把用户抖一抖的次数累加存入缓存
        /// </summary>
        /// <param name="id">抖一抖活动实例序号</param>
        /// <param name="number">次数</param>
        /// <returns></returns>
        public async Task<int> Push(int id, int number)
        {
            if (number <= 0 && id <= 0) return 0;

            List<SUser> lstUser = new List<SUser>();
            _users.TryGetValue("Mobile" + id, out lstUser);
            if (lstUser != null && lstUser.Any())
            {
                var user = lstUser.Find(i => i.ConnectionId == Context.ConnectionId);
                if (user != null)
                {
                    user.Count += number;
                    _users.GetOrAdd("Mobile" + id, lstUser);
                    string pcid = GetCache("PC" + id) as string;
                    if (!string.IsNullOrEmpty(pcid as string)) await Clients.Client(pcid).MyCount(user); //推送给PC 展示参与进来的移动端信息 和当前在线总数量
                    return user.Count;
                }
                throw new HubException("用户数据不存在");
            }
            throw new HubException("此游戏不成立");
        }

        #endregion

        #region 用户实体

        /// <summary>
        /// 抖一抖移动用户实体
        /// </summary>
        public class SUser
        {
            /// <summary>
            /// 序号
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// 用户名字
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 用户头像
            /// </summary>
            public string Img { get; set; }

            /// <summary>
            /// 抖一抖次数
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// 当前身份的唯一标示
            /// </summary>
            public string ConnectionId { get; set; }
        }

        #endregion
    }
}