using System.Collections.Generic;
using PinkUmbrella.Models;
using PinkUmbrella.Models.Community;
using PinkUmbrella.Models.Public;
using Tides.Models;
using Tides.Models.Public;

namespace PinkUmbrella.ViewModels.Profile
{
    public class IndexViewModel: ProfileViewModel
    {
        public FeedModel Feed { get; set; }

        public PaginatedModel<ArchivedMediaModel> Media { get; set; }

        public List<ShopModel> Shops { get; set; }
        
        public PublicProfileModel[] Users { get; set; }
        
        public UserListType UserListType { get; set; }
    }
}