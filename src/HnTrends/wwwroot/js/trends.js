var categories = ["Day", "Week", "Month"];

var count = "Count";
var percent = "Percent";
var score = "Score";

document.states = {
    Count: {
        next: percent,
        label: '# of stories',
        nextButtonText: 'Display as % of stories'
    },
    Percent: {
        next: score,
        label: '% of stories',
        nextButtonText: 'Display as score of stories'
    },
    Score: {
        next: count,
        label: 'Story score (sum)',
        nextButtonText: 'Display as # of stories'
    }
}

function addDays(date, days) {
    var result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
}

function loadDataFromHiddenInput() {
    var str = $("#data-load").val();
    var json = JSON.parse(str);
    var start = window.moment(json.Start);

    var dates = json.Counts.map(() => {
        var date = start.toDate();
        start.add(1, 'd');
        return date;
    });

    json.Dates = dates;

    return json;
}

function buildXAxis(data, period) {
    var last = data.x[data.x.length - 1];
    var max = period !== "Day"
        ? last
        : addDays(Date.parse(last), 1);

    return {
        range: [data.x[0], max],
        type: 'date',
        linecolor: '#333',
        linewidth: 1,
        title: period
    };
}

function buildYAxis(data, trendType) {
    var maxBySeries = trendType === count
        ? data.ys.map(series => Math.max.apply(Math, series))
        : trendType === percent ? data.percents.map(series => Math.max.apply(Math, series))
            : data.scores.map(series => Math.max.apply(Math, series));

    var label = document.states[trendType].label;

    var max = Math.max.apply(Math, maxBySeries);
    return {
        range: [0, trendType !== percent ? (max + 1) : max + 0.1],
        linecolor: '#333',
        linewidth: 1,
        title: label
    };
}

function getPlotlyLayout(data, trendType, period) {
    return {
        xaxis: buildXAxis(data, period),
        yaxis: buildYAxis(data, trendType),
        title: `Posts over time for '${data.names[0]}'.`,
        hovermode: 'closest'
    };
}

function getModeBarSettings() {
    return {
        modeBarButtonsToRemove: ['pan2d', 'lasso2d', 'autoScale2d', 'toggleSpikelines', 'select2d']
    };
}

function getSkipLast() {
    return $("#skiplast").prop('checked');
}

function dataForPeriod(data, others, period, skipLast) {
    var dateLabels = [];
    var counts = [[]];
    var percents = [[]];
    var scores = [[]];
    var names = [data.Term];

    others.forEach(x => {
        counts.push([]);
        percents.push([]);
        scores.push([]);
        names.push(x.term + (x.allWords ? '' : ' (any)'));
    });

    var previous;

    data.Dates.forEach((x, i) => {
        switch (period) {
            case "Week":
                groupDataPointsByPeriod(data,
                    x,
                    i,
                    dateLabels,
                    others,
                    counts,
                    percents,
                    scores,
                    previous,
                    x => window.moment(x).endOf('week').toDate());
                previous = x;
                break;
            case "Month":
                groupDataPointsByPeriod(data,
                    x,
                    i,
                    dateLabels,
                    others,
                    counts,
                    percents,
                    scores,
                    previous,
                    x => window.moment(x).startOf('month').toDate());
                previous = x;
                break;
            case "Day":
            default:
                dateLabels.push(x);

                counts[0].push(data.Counts[i]);
                scores[0].push(data.Scores[i]);
                percents[0].push((data.Counts[i] / data.DailyTotals[i]) * 100);
                others.forEach((o, oi) => {
                    counts[oi + 1].push(o.counts[i]);
                    scores[oi + 1].push(o.scores[i]);
                    percents[oi + 1].push(((o.counts[i] / data.DailyTotals[i]) * 100).toFixed(2));
                });
                break;
        }
    });

    if (period !== "Day") {
        counts.forEach((set, setIndex) => {
            var percentData = percents[setIndex];
            set.forEach((x, i) => {
                if (percentData[i] !== 0) {
                    percentData[i] = ((x / percentData[i]) * 100).toFixed(2);
                }
            });
        });
    }

    if (skipLast) {
        dateLabels.pop();
        counts.forEach(x => x.pop());
        percents.forEach(x => x.pop());
    }

    return {
        ys: counts,
        percents: percents,
        scores: scores,
        x: dateLabels,
        names: names
    };
}

function groupDataPointsByPeriod(data,
    date,
    index,
    dateLabels,
    others,
    counts,
    percents,
    scores,
    previous,
    periodGrouper) {
    var dayCount = data.Counts[index];
    var dayTotal = data.DailyTotals[index];
    var dayScore = data.Scores[index];

    var isNewPeriod = false;
    var periodForThisDate = periodGrouper(date);

    if (index === 0) {
        dateLabels.push(periodForThisDate);
        isNewPeriod = true;
    } else {
        var periodForPreviousDate = periodGrouper(previous);

        if (periodForThisDate.toDateString() !== periodForPreviousDate.toDateString()) {
            dateLabels.push(periodForThisDate);
            isNewPeriod = true;
        }
    }

    if (isNewPeriod) {
        counts[0].push(dayCount);
        percents[0].push(dayTotal);
        scores[0].push(dayScore);

        others.forEach((o, oi) => {
            counts[oi + 1].push(o.counts[index]);
            percents[oi + 1].push(dayTotal);
            scores[oi + 1].push(o.scores[index]);
        });
    } else {
        var periodIndex = counts[0].length - 1;
        counts[0][periodIndex] += dayCount;
        percents[0][periodIndex] += dayTotal;
        scores[0][periodIndex] += dayScore;
        others.forEach((o, oi) => {
            counts[oi + 1][periodIndex] += o.counts[index];
            percents[oi + 1][periodIndex] += dayTotal;
            scores[oi + 1][periodIndex] += dayScore;
        });
    }
}

function getPlotlyData(data, trendType, period) {
    var isMonthly = period === "Month";
    var width = !isMonthly ? 1 : data.ys.length === 1 ? 2 : 1;
    return data.ys.map((ySeries, i) => {
        return {
            x: data.x,
            y: trendType === count ? ySeries : trendType === percent ? data.percents[i] : data.scores[i],
            type: 'scatter',
            mode: period !== "Day" ? 'lines' : 'markers',
            marker: {
                size: 3
            },
            line: {
                width: width
            },
            name: data.names[i]
        };
    });
}

function getPlotlyElement() {
    return document.getElementById('plot');
}

function periodSelectorChanged() {
    var period = $("#period-selector").val();

    if (categories.indexOf(period) < 0) {
        return;
    }

    togglePeriod(period);
}

function onSkipLastChange() {
    togglePeriod(document.hntrendstore.CurrentCategory);
}

function togglePeriod(period) {
    document.hntrendstore.CurrentCategory = period;

    $("#period").text(period);

    var trendType = document.hntrendstore.trendType;
    var data = dataForPeriod(loadDataFromHiddenInput(),
        document.hntrendstore.others,
        document.hntrendstore.CurrentCategory,
        getSkipLast());
    
    window.Plotly.newPlot(getPlotlyElement(),
        getPlotlyData(data, trendType, document.hntrendstore.CurrentCategory),
        getPlotlyLayout(data, trendType, document.hntrendstore.CurrentCategory),
        getModeBarSettings());
}

function switchTrendType() {
    var current = document.hntrendstore.trendType;
    var currentState = document.states[current];
    var newTrendType = currentState.next;

    var newState = document.states[newTrendType];

    $('#percent').text(newState.nextButtonText);
    document.hntrendstore.trendType = newTrendType;
    var data = dataForPeriod(loadDataFromHiddenInput(),
        document.hntrendstore.others,
        document.hntrendstore.CurrentCategory,
        getSkipLast());
    
    window.Plotly.newPlot(getPlotlyElement(),
        getPlotlyData(data, newTrendType, document.hntrendstore.CurrentCategory),
        getPlotlyLayout(data, newTrendType, document.hntrendstore.CurrentCategory),
        getModeBarSettings());
}

function addDataPlot(data) {
    document.hntrendstore.others.push(data);
    togglePeriod(document.hntrendstore.CurrentCategory);
}

$("#add-term").click(() => {
    var term = $("#text").val().trim();

    if (term.length < 1) {
        $("#text").val('');
        return;
    }

    var allWords = $("#allwords").prop('checked');

    var hasExisting = document.hntrendstore.others.filter(x => x.term.toUpperCase() === term.toUpperCase()
        && x.allWords === allWords)
        .length >
        0;

    if (hasExisting) {
        alert(`Search term ${term} already displayed.`);
        return;
    }

    $("#add-term").hide();
    $("#loading").show();

    $.get(`/api/plot/${encodeURIComponent(term)}?allWords=${allWords}`)
        .done(data => {
            $("#text").val('');
            addDataPlot(data);
            $("#terms-list-list").append(`<li><a href="/api/results/${encodeURIComponent(term)}" target="_blank">${term}</a></li>`);
        })
        .fail(err => {
            console.log(err);
            alert('An error occurred, please check the log.');
        })
        .always(() => {
            $("#add-term").show();
            $("#loading").hide();
        });
});

$("#text").keyup(event => {
    if (event.keyCode === 13) {
        $("#add-term").click();
    }
});

$(function () {

    document.hntrendstore = {
        trendType: count,
        CurrentCategory: "Month",
        others: []
    };

    if (!$('#data-load').length) {
        return;
    }

    $("#period").text(document.hntrendstore.CurrentCategory);

    var shouldSkipLast = window.moment().date() < 10;

    if (shouldSkipLast) {
        $("#skiplast").click();
    }

    $("#skiplast").on('change', onSkipLastChange);

    var json = loadDataFromHiddenInput();

    var data = dataForPeriod(json, [], document.hntrendstore.CurrentCategory, getSkipLast());
    
    window.Plotly.plot(getPlotlyElement(),
        getPlotlyData(data, count, document.hntrendstore.CurrentCategory),
        getPlotlyLayout(data, count, document.hntrendstore.CurrentCategory),
        getModeBarSettings());
});