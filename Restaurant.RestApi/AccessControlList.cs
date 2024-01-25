/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurants.RestApi
{
    /// <summary>
    /// 식당 예제를 위한 접근 제어 목록
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이 부분은 전통적인 파일 디스크립터의 측면에서는 접근 제어 
    /// 목록이라 부르기 어려운 부분이 있을 수 있습니다. 
    /// 하지만, 특정 사용자가 접근할 수 있는 restaurant ID 목록을 
    /// 제공해주고 있습니다. 
    /// </para>
    /// </remarks>
    public sealed class AccessControlList
    {
        private readonly IReadOnlyCollection<int> restaurantIds;

        public AccessControlList(IReadOnlyCollection<int> restaurantIds)
        {
            this.restaurantIds = restaurantIds;
        }

        public AccessControlList(params int[] restaurantIds) :
            this(restaurantIds.ToList())
        {
        }

        internal bool Authorize(int restaurantId)
        {
            return restaurantIds.Contains(restaurantId);
        }

        internal static AccessControlList FromUser(ClaimsPrincipal user)
        {
            var restaurantIds = user
                .FindAll("restaurant")
                .SelectMany(c => ClaimToRestaurantId(c))
                .ToList();
            return new AccessControlList(restaurantIds);
        }

        private static int[] ClaimToRestaurantId(Claim claim)
        {
            if (int.TryParse(claim.Value, out var i))
                return new[] { i };
            return Array.Empty<int>();
        }
    }
}
