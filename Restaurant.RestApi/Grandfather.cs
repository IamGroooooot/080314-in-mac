﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurants.RestApi
{
    /// <summary>
    /// 시스템이 멀티테넌트 시스템으로 확장될 때 할아버지 클랙스가 된 
    /// 레스토랑 클래스에 대한 정보를 가지고 있습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 이 코드 베이스는 원래 하나의 레스토랑을 지원하기 위해서 만들어진 것입니다. 
    /// 이런 맥락에서 레스토랑 클래스는 명시적인 엔티티가 아닌 암시적인 부분으로 
    /// 존재했습니다. 하지만, 멀티테넌트 시스템으로 확장할 때 원래의 레스토랑 
    /// 클래스는 할아버지 클래스 역할을 해야 합니다. 따라서, 특정한 레스토랑에서는
    /// 명시적으로 동작하지 않는 API의 경우, 기존에 있던 타사의 API를 손상시키지
    /// 않기 위해서 암시적으로 "첫번째" 레스토랑에서 동작한다고 가정할 것입니다.
    /// </para>
    /// </remarks>
    public static class Grandfather
    {
        /// <summary>
        /// '오리지널' 레스토랑의 ID로써, 임의로 특정 번호를 지정합니다. 
        /// 레스토랑 구성에 있어서 이 ID를 가진 레스토랑은 반드시 존재해야 
        /// 합니다.
        /// </summary>
        public const int Id = 1;
    }
}
