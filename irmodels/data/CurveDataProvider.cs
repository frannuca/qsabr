﻿using System;

        public override Series<DateTime, double> getTargetSeries(string ticker)
        {
            var a = from p in _data where p.curve_label == ticker select p;
            var b = Frame.FromRecords(a).IndexRows<DateTime>("date");
            var targetts= b.GetColumn<double>("curve_value");
            var dates = CleanRiskFrame.RowKeys;
            return targetts[dates];
        }

        protected override Frame<DateTime, string> toframe(CURVER_TPYE curve_type)