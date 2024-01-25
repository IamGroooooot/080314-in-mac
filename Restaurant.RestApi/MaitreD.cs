/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ploeh.Samples.Restaurants.RestApi
{
    public sealed class MaitreD
    {
        public MaitreD(
            TimeOfDay opensAt,
            TimeOfDay lastSeating,
            TimeSpan seatingDuration,
            params Table[] tables) :
            this(opensAt, lastSeating, seatingDuration, tables.AsEnumerable())
        {
        }

        public MaitreD(
            TimeOfDay opensAt,
            TimeOfDay lastSeating,
            TimeSpan seatingDuration,
            IEnumerable<Table> tables)
        {
            OpensAt = opensAt;
            LastSeating = lastSeating;
            SeatingDuration = seatingDuration;
            Tables = tables;
        }

        public TimeOfDay OpensAt { get; }
        public TimeOfDay LastSeating { get; }
        public TimeSpan SeatingDuration { get; }
        public IEnumerable<Table> Tables { get; }

        public MaitreD WithOpensAt(TimeOfDay newOpensAt)
        {
            return
                new MaitreD(newOpensAt, LastSeating, SeatingDuration, Tables);
        }

        public MaitreD WithLastSeating(TimeOfDay newLastSeating)
        {
            return
                new MaitreD(OpensAt, newLastSeating, SeatingDuration, Tables);
        }

        public MaitreD WithSeatingDuration(TimeSpan newSeatingDuration)
        {
            return
                new MaitreD(OpensAt, LastSeating, newSeatingDuration, Tables);
        }

        public MaitreD WithTables(params Table[] newTables)
        {
            return
                new MaitreD(OpensAt, LastSeating, SeatingDuration, newTables);
        }

        public bool WillAccept(
            DateTime now,
            IEnumerable<Reservation> existingReservations,
            Reservation candidate)
        {
            if (existingReservations is null)
                throw new ArgumentNullException(nameof(existingReservations));
            if (candidate is null)
                throw new ArgumentNullException(nameof(candidate));
            if (candidate.At < now)
                return false;
            if (IsOutsideOfOpeningHours(candidate))
                return false;

            var seating = new Seating(SeatingDuration, candidate.At);
            var relevantReservations =
                existingReservations.Where(seating.Overlaps);
            var availableTables = Allocate(relevantReservations);
            return availableTables.Any(t => t.Fits(candidate.Quantity));
        }

        private bool IsOutsideOfOpeningHours(Reservation reservation)
        {
            return reservation.At.TimeOfDay < OpensAt
                || LastSeating < reservation.At.TimeOfDay;
        }

        private IEnumerable<Table> Allocate(
            IEnumerable<Reservation> reservations)
        {
            List<Table> allocation = Tables.ToList();
            foreach (var r in reservations)
            {
                var table = allocation.Find(t => t.Fits(r.Quantity));
                if (table is { })
                {
                    allocation.Remove(table);
                    allocation.Add(table.Reserve(r));
                }
            }

            return allocation;
        }

        public IEnumerable<TimeSlot> Schedule(
            IEnumerable<Reservation> reservations)
        {
            return
                from r in reservations
                group r by r.At into g
                orderby g.Key
                let seating = new Seating(SeatingDuration, g.Key)
                let overlapping = reservations.Where(seating.Overlaps)
                select new TimeSlot(g.Key, Allocate(overlapping).ToList());
        }

        /// <summary>
        /// 레스토랑 영업 시간 내에서 하루를 15분 단위로 나누고
        /// 각 시간대별로 테이블을 구성합니다. 
        /// </summary>
        /// <param name="date">분할할 날짜</param>
        /// <param name="reservations">
        /// <paramref name="date" />에 관련된 예약.
        /// </param>
        /// <returns>
        /// 레스토랑이 열었을 때부터 시작해서 레스토랑의 마지막 좌석 배치 
        /// 시간이 끝날때까지를 15분 단위로 분할해서 반환합니다.
        /// 분할된 각각의 시간에는 해당 시간대의 테이블 배치가 포함됩니다.
        /// </returns>
        public IEnumerable<TimeSlot> Segment(
            DateTime date,
            IEnumerable<Reservation> reservations)
        {
            for (var dur = (TimeSpan)OpensAt;
                 dur <= (TimeSpan)LastSeating;
                 dur = dur.Add(TimeSpan.FromMinutes(15)))
            {
                var at = date.Date.Add(dur);
                var seating = new Seating(SeatingDuration, at);
                var relevantReservations =
                    reservations.Where(seating.Overlaps);
                var allocation = Allocate(relevantReservations);
                yield return new TimeSlot(at, allocation.ToList());
            }
        }
    }
}