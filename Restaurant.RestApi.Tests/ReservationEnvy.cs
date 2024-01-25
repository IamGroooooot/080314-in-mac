/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Text;

namespace Ploeh.Samples.Restaurants.RestApi.Tests
{
    public static class ReservationEnvy
    {
        // 이 메서드의 경우 테스트에는 유용하지만, 프로덕션 코드에 위치할 부분은
        // 아닌 것 같습니다. 시스템에서 예약 ID를 바꾸려할 이유가 있을까요?
        // 타당한 이유가 있는 것이 확인된다면, 이 함수를 Reservation 클래스로 
        // 이동시키는 것을 고려하십시오. 
        public static Reservation WithId(
            this Reservation reservation,
            Guid newId)
        {
            if (reservation is null)
                throw new ArgumentNullException(nameof(reservation));

            return new Reservation(
                newId,
                reservation.At,
                reservation.Email,
                reservation.Name,
                reservation.Quantity);
        }

        public static Reservation AddDate(
            this Reservation reservation,
            TimeSpan timeSpan)
        {
            if (reservation is null)
                throw new ArgumentNullException(nameof(reservation));

            return reservation.WithDate(reservation.At.Add(timeSpan));
        }

        public static Reservation OneHourBefore(this Reservation reservation)
        {
            return reservation.AddDate(TimeSpan.FromHours(-1));
        }

        public static Reservation TheDayBefore(this Reservation reservation)
        {
            return reservation.AddDate(TimeSpan.FromDays(-1));
        }

        public static Reservation OneHourLater(this Reservation reservation)
        {
            return reservation.AddDate(TimeSpan.FromHours(1));
        }

        public static Reservation TheDayAfter(this Reservation reservation)
        {
            return reservation.AddDate(TimeSpan.FromDays(1));
        }
    }
}
