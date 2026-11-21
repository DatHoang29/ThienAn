export function generateRandomNumber(length) {
    let number = "";
    for (let i = 0; i < length; i++) {
        number += Math.floor(Math.random() * 10);
    }
    return number;
}