(() => {
    "use strict";

    const root =
        document.querySelector(
            '[data-analytics-live="true"]'
        );

    if (!root ||
        typeof window.signalR === "undefined") {
        return;
    }

    const storageKey =
        "edulytics.analytics.liveUpdate";

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/analytics")
            .withAutomaticReconnect()
            .build();

    let refreshScheduled = false;

    connection.on(
        "AnalyticsUpdated",
        () => {
            if (refreshScheduled) {
                return;
            }

            refreshScheduled = true;

            sessionStorage.setItem(
                storageKey,
                "1"
            );

            window.setTimeout(
                () => window.location.reload(),
                120
            );
        });

    connection.onreconnecting(() => {
        document.documentElement.dataset
            .analyticsRealtime =
            "reconnecting";
    });

    connection.onreconnected(() => {
        document.documentElement.dataset
            .analyticsRealtime =
            "connected";
    });

    connection.onclose(() => {
        document.documentElement.dataset
            .analyticsRealtime =
            "disconnected";
    });

    connection
        .start()
        .then(() => {
            document.documentElement.dataset
                .analyticsRealtime =
                "connected";
        })
        .catch(() => {
            document.documentElement.dataset
                .analyticsRealtime =
                "disconnected";
        });
})();
