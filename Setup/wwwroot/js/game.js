function renderGameCards() {
    const coll2 = document.getElementsByClassName("big-card-overlay");

    for (var i = 0; i < coll2.length; i++) {
        let identifier = 1;

        coll2[i].style.marginTop = (i * 3) / identifier + 10 + "%";
        coll2[i].style.marginLeft = (i * 10) / identifier + 5 + "%";
    }
}

const coll = document.getElementsByClassName("overlay");

for (var i = 0; i < coll.length; i++) {
    let identifier = 1;
    if (coll.length > 4) {
        identifier += (coll.length - 4) * 0.35;
    }


    coll[i].style.marginTop = (i * 0.75) / identifier + "%";
    coll[i].style.marginLeft = (i * 4) / identifier + 1 + "%";

    coll[i].style.height = ((identifier * 0.1) + 8) + "%";
    coll[i].style.width = ((identifier * 0.25) + 10) + "%";

    coll[i].childNodes[1].style.marginTop = ((identifier * 5) + 70) + "%";
}

