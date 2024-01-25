/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Ploeh.Samples.Restaurants.RestApi.Tests
{
    public sealed class MaitreDSegmentTests
    {
        [Property]
        public Property Segment()
        {
            return Prop.ForAll(
                (from rs in Gens.Reservations
                 from m in Gens.MaitreD(rs)
                 from d in GenDate(rs)
                 select (m, d, rs)).ToArbitrary(),
                t => SegmentImp(t.m, t.d, t.rs));
        }

        private static void SegmentImp(
            MaitreD sut,
            DateTime date,
            Reservation[] reservations)
        {
            var actual = sut.Segment(date, reservations);

            Assert.NotEmpty(actual);
            Assert.Equal(
                date.Date.Add((TimeSpan)sut.OpensAt),
                actual.First().At);
            Assert.Equal(
                date.Date.Add((TimeSpan)sut.LastSeating),
                actual.Last().At);
            AssertFifteenMinuteDistances(actual);
            Assert.All(actual, ts => AssertTables(sut.Tables, ts.Tables));
            Assert.All(
                actual,
                ts => AssertRelevance(reservations, sut.SeatingDuration, ts));
        }

        private static void AssertFifteenMinuteDistances(
            IEnumerable<TimeSlot> actual)
        {
            var times = actual.Select(ts => ts.At).OrderBy(t => t);
            var deltas = times.Zip(times.Skip(1), (x, y) => y - x);
            Assert.All(deltas, d => Assert.Equal(TimeSpan.FromMinutes(15), d));
        }

        private static void AssertTables(
           IEnumerable<Table> expected,
           IEnumerable<Table> actual)
        {
            Assert.Equal(expected.Count(), actual.Count());
            Assert.Equal(
                expected.Sum(t => t.Capacity),
                actual.Sum(t => t.Capacity));
        }

        private static void AssertRelevance(
            IEnumerable<Reservation> reservations,
            TimeSpan seatingDuration,
            TimeSlot timeSlot)
        {
            var seating = new Seating(seatingDuration, timeSlot.At);
            var expected = reservations
                .Select(r => (new Seating(seatingDuration, r.At), r))
                .Where(t => seating.Overlaps(t.Item1))
                .Select(t => t.r)
                .ToHashSet();

            var actual = timeSlot.Tables
                .SelectMany(t => t.Accept(new ReservationsVisitor()))
                .ToHashSet();

            Assert.True(
                expected.SetEquals(actual),
                $"Expected: {expected}; actual {actual}.");
        }

        private sealed class ReservationsVisitor :
            ITableVisitor<IEnumerable<Reservation>>
        {
            public IEnumerable<Reservation> VisitCommunal(
                int seats,
                IReadOnlyCollection<Reservation> reservations)
            {
                return reservations;
            }

            public IEnumerable<Reservation> VisitStandard(
                int seats,
                Reservation? reservation)
            {
                if (reservation is { })
                    yield return reservation;
            }
        }

        /// <summary>
        /// 제약이 없는 임의의 날짜를 만들거나, 
        /// <paramref name="reservations" />에서 선택한 날짜를
        /// 생성합니다.
        /// </summary>
        /// <param name="reservations">
        /// 예약 날짜를 선택할 수 있도록 하는 부분
        /// </param>
        /// <returns>
        /// 여기서는 <paramref name="reservations" />에서 받아온 
        /// 날짜중에서 하나의 날짜를 무작위로 선택해서 반환합니다.
        /// 예약 컬렉션이 비어있으면 임의의 날짜가 반환되며, 
        /// 비어있지 않은 경우에도 이런 일이 발생할 수 있습니다. 
        /// 확률은 50%입니다. 
        /// </returns>
        private static Gen<DateTime> GenDate(
            IEnumerable<Reservation> reservations)
        {
            var randomDayGen = Arb.Default.DateTime().Generator;
            if (!reservations.Any())
                return randomDayGen;

            var oneOfReservationsDayGet = Gen.Elements(reservations
                .Select(r => r.At));

            return Gen.OneOf(randomDayGen, oneOfReservationsDayGet);            
        }
    }
}
