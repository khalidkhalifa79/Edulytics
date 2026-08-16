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

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl("/hubs/analytics")
            .withAutomaticReconnect()
            .build();

    const refreshDebounceMs = 450;
    let refreshTimer = null;

    function scheduleAuthoritativeRefresh(reason) {
        if (refreshTimer !== null) {
            window.clearTimeout(refreshTimer);
        }

        document.documentElement.dataset
            .analyticsRealtimeReason =
            reason;

        refreshTimer =
            window.setTimeout(
                () => {
                    // SignalR is only an invalidation hint.
                    // The MVC GET remains the authoritative state.
                    window.location.reload();
                },
                refreshDebounceMs
            );
    }

    connection.on(
        "AnalyticsUpdated",
        () => {
            scheduleAuthoritativeRefresh(
                "event"
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

        // Events can be missed during disconnect.
        scheduleAuthoritativeRefresh(
            "reconnect"
        );
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
