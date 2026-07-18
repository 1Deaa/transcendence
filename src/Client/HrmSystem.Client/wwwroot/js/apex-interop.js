/*
    //?     ES-module JS interop for ApexChart.razor — the only bridge between Blazor and
    //?     the ApexCharts global loaded from assets/vendor/libs/apex-charts/apexcharts.js.
    //?     Charts are tracked by element id so Blazor can update/destroy without leaking.
*/
const charts = new Map();

export function render(elementId, optionsJson) {
    const element = document.getElementById(elementId);
    if (!element) {
        return;
    }

    destroy(elementId);

    const options = JSON.parse(optionsJson);
    const chart = new ApexCharts(element, options);
    chart.render();
    charts.set(elementId, chart);
}

export function updateSeries(elementId, seriesJson) {
    const chart = charts.get(elementId);
    if (chart) {
        chart.updateSeries(JSON.parse(seriesJson), true);
    }
}

export function destroy(elementId) {
    const chart = charts.get(elementId);
    if (chart) {
        chart.destroy();
        charts.delete(elementId);
    }
}

/*
    //?     Client-side file download for authenticated exports: the bytes arrive via
    //?     HttpClient (bearer header attached), then this turns them into a save dialog.
*/
export function downloadFile(fileName, contentType, base64Content) {
    const byteCharacters = atob(base64Content);
    const bytes = new Uint8Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        bytes[i] = byteCharacters.charCodeAt(i);
    }

    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
}
