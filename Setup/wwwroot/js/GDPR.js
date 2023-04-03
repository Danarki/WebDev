class GDPR {
    constructor() {
        this.showStatus();

        this.bindEvents();

        if (this.getCookieStatus()) this.hideGDPR()
    }

    setCookieStatus(status) {
        if (status !== null) localStorage.setItem('gdpr-status', status);
    }

    getCookieStatus() {
        return localStorage.getItem('gdpr-status');
    }

    showStatus() {
        let statusMessage;
        switch (this.getCookieStatus()) {
            case 'accepted':
                statusMessage = 'Accepted';
                break;
            case 'declined':
                statusMessage = 'Declined'; 
                break;
            default:
                statusMessage = 'Not chosen';
        }

        document.getElementById('gdpr__status').innerHTML = statusMessage;
    }

    bindEvents() {
        const acceptButton = document.querySelector(".gdpr__buttons__accept");
        acceptButton.addEventListener('click', () => {
            this.setCookieStatus('accepted');
            this.showStatus();
            this.hideGDPR();
        })

        const declineButton = document.querySelector(".gdpr__buttons__decline");
        declineButton.addEventListener('click', () => {
            this.setCookieStatus('declined');
            this.showStatus();
            this.hideGDPR();
        })
    }

    hideGDPR() {
        document.querySelector('.gdpr').classList.add('hide');
        document.querySelector('.gdpr').classList.remove('show');
    }
}

const gdpr = new GDPR();