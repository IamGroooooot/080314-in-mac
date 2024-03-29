﻿/* Copyright (c) Mark Seemann 2020. All rights reserved. */
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace Ploeh.Samples.Restaurants.RestApi.Tests
{
    public sealed class MaitreDTests
    {
        [SuppressMessage(
            "Performance",
            "CA1812: Avoid uninstantiated internal classes",
            Justification = "This class is instantiated via Reflection.")]
        private class AcceptTestCases :
            TheoryData<MaitreD, DateTime, IEnumerable<Reservation>>
        {
            public AcceptTestCases()
            {
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(12) }),
                    Some.Now,
                    Array.Empty<Reservation>());
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(8), Table.Communal(11) }),
                    Some.Now,
                    Array.Empty<Reservation>());
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(2), Table.Communal(11) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(2) });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(11) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(11).TheDayBefore() });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(11) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(11).TheDayAfter() });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(2.5),
                        new[] { Table.Standard(12) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(11).AddDate(
                        TimeSpan.FromHours(-2.5)) });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(1),
                        new[] { Table.Standard(14) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(9).AddDate(
                        TimeSpan.FromHours(1)) });
            }
        }

        [SuppressMessage(
            "Design",
            "CA1062:Validate arguments of public methods",
            Justification = "Parametrised test.")]
        [Theory, ClassData(typeof(AcceptTestCases))]
        public void Accept(
            MaitreD sut,
            DateTime now,
            IEnumerable<Reservation> reservations)
        {
            var r = Some.Reservation.WithQuantity(11);
            var actual = sut.WillAccept(now, reservations, r);
            Assert.True(actual);
        }

        [SuppressMessage(
            "Performance",
            "CA1812: Avoid uninstantiated internal classes",
            Justification = "This class is instantiated via Reflection.")]
        private class RejectTestCases :
            TheoryData<MaitreD, DateTime, IEnumerable<Reservation>>
        {
            public RejectTestCases()
            {
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Communal(6), Table.Communal(6) }),
                    Some.Now,
                    Array.Empty<Reservation>());
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Standard(12) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(1) });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Standard(11) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(1).OneHourBefore() });
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Standard(12) }),
                    Some.Now,
                    new[] { Some.Reservation.WithQuantity(2).OneHourLater() });
                /* Some.Reservation.At은 아래 테스트에서 하드코딩된 예약 시간입니다.
                 * 여기에 30분을 더해지면, 원하는 예약 시간보다 30분 늦게 레스토랑을
                 * 열어야 하기 때문에, 예약이 거부되어야 합니다.  */
                Add(new MaitreD(
                        Some.Reservation.At.AddMinutes(30).TimeOfDay,
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        new[] { Table.Standard(12) }),
                    Some.Now,
                    Array.Empty<Reservation>());
                /* Some.Reservation.At은 아래 테스트에서 하드코딩된 예약 시간입니다.
                 * 여기서 30분을 빼면, 예약 가능 시간 30분 전에 레스토랑의 마지막 좌석이 
                 * 예약되어야 한다는 의미이므로, 예약이 거부되어 합니다. */
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        Some.Reservation.At.AddMinutes(-30).TimeOfDay,
                        TimeSpan.FromHours(6),
                        new[] { Table.Standard(12) }),
                    Some.Now,
                    Array.Empty<Reservation>());
                Add(new MaitreD(
                        TimeSpan.FromHours(18),
                        TimeSpan.FromHours(21),
                        TimeSpan.FromHours(6),
                        Table.Standard(12)),
                    Some.Now.AddDays(30),
                    Array.Empty<Reservation>());
            }
        }

        [SuppressMessage(
            "Design",
            "CA1062:Validate arguments of public methods",
            Justification = "Parametrised test.")]
        [Theory, ClassData(typeof(RejectTestCases))]
        public void Reject(
            MaitreD sut,
            DateTime now,
            IEnumerable<Reservation> reservations)
        {
            var r = Some.Reservation.WithQuantity(11);
            var actual = sut.WillAccept(now, reservations, r);
            Assert.False(actual);
        }

        [Fact]
        public void ScheduleNoReservations()
        {
            var sut = new MaitreD(
                TimeSpan.FromHours(18),
                TimeSpan.FromHours(21),
                TimeSpan.FromHours(6),
                Table.Communal(12));

            var actual = sut.Schedule(Enumerable.Empty<Reservation>());

            var expected = Enumerable.Empty<TimeSlot>();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ScheduleSingleReservationCommunalTable()
        {
            var table = Table.Communal(12);
            var sut = new MaitreD(
                TimeSpan.FromHours(18),
                TimeSpan.FromHours(21),
                TimeSpan.FromHours(6),
                table);

            var r = Some.Reservation;
            var actual = sut.Schedule(new[] { r });

            var expected = new[] { new TimeSlot(r.At, table.Reserve(r)) };
            Assert.Equal(expected, actual);
        }
    }
}
