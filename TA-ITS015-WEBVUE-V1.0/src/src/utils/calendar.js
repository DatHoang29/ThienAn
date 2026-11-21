/* eslint-disable */

var calendar = (function () {
  'use strict';

  var lunarInfo = [0x04bd8, 0x04ae0, 0x0a570, 0x054d5, 0x0d260, 0x0d950, 0x16554, 0x056a0, 0x09ad0, 0x055d2,
  //1900-1909
  0x04ae0, 0x0a5b6, 0x0a4d0, 0x0d250, 0x1d255, 0x0b540, 0x0d6a0, 0x0ada2, 0x095b0, 0x14977,
  //1910-1919
  0x04970, 0x0a4b0, 0x0b4b5, 0x06a50, 0x06d40, 0x1ab54, 0x02b60, 0x09570, 0x052f2, 0x04970,
  //1920-1929
  0x06566, 0x0d4a0, 0x0ea50, 0x16a95, 0x05ad0, 0x02b60, 0x186e3, 0x092e0, 0x1c8d7, 0x0c950,
  //1930-1939
  0x0d4a0, 0x1d8a6, 0x0b550, 0x056a0, 0x1a5b4, 0x025d0, 0x092d0, 0x0d2b2, 0x0a950, 0x0b557,
  //1940-1949
  0x06ca0, 0x0b550, 0x15355, 0x04da0, 0x0a5b0, 0x14573, 0x052b0, 0x0a9a8, 0x0e950, 0x06aa0,
  //1950-1959
  0x0aea6, 0x0ab50, 0x04b60, 0x0aae4, 0x0a570, 0x05260, 0x0f263, 0x0d950, 0x05b57, 0x056a0,
  //1960-1969
  0x096d0, 0x04dd5, 0x04ad0, 0x0a4d0, 0x0d4d4, 0x0d250, 0x0d558, 0x0b540, 0x0b6a0, 0x195a6,
  //1970-1979
  0x095b0, 0x049b0, 0x0a974, 0x0a4b0, 0x0b27a, 0x06a50, 0x06d40, 0x0af46, 0x0ab60, 0x09570,
  //1980-1989
  0x04af5, 0x04970, 0x064b0, 0x074a3, 0x0ea50, 0x06b58, 0x05ac0, 0x0ab60, 0x096d5, 0x092e0,
  //1990-1999
  0x0c960, 0x0d954, 0x0d4a0, 0x0da50, 0x07552, 0x056a0, 0x0abb7, 0x025d0, 0x092d0, 0x0cab5,
  //2000-2009
  0x0a950, 0x0b4a0, 0x0baa4, 0x0ad50, 0x055d9, 0x04ba0, 0x0a5b0, 0x15176, 0x052b0, 0x0a930,
  //2010-2019
  0x07954, 0x06aa0, 0x0ad50, 0x05b52, 0x04b60, 0x0a6e6, 0x0a4e0, 0x0d260, 0x0ea65, 0x0d530,
  //2020-2029
  0x05aa0, 0x076a3, 0x096d0, 0x04afb, 0x04ad0, 0x0a4d0, 0x1d0b6, 0x0d250, 0x0d520, 0x0dd45,
  //2030-2039
  0x0b5a0, 0x056d0, 0x055b2, 0x049b0, 0x0a577, 0x0a4b0, 0x0aa50, 0x1b255, 0x06d20, 0x0ada0,
  //2040-2049
  /**Add By JJonline@JJonline.Cn**/
  0x14b63, 0x09370, 0x049f8, 0x04970, 0x064b0, 0x168a6, 0x0ea50, 0x06b20, 0x1a6c4, 0x0aae0,
  //2050-2059
  0x092e0, 0x0d2e3, 0x0c960, 0x0d557, 0x0d4a0, 0x0da50, 0x05d55, 0x056a0, 0x0a6d0, 0x055d4,
  //2060-2069
  0x052d0, 0x0a9b8, 0x0a950, 0x0b4a0, 0x0b6a6, 0x0ad50, 0x055a0, 0x0aba4, 0x0a5b0, 0x052b0,
  //2070-2079
  0x0b273, 0x06930, 0x07337, 0x06aa0, 0x0ad50, 0x14b55, 0x04b60, 0x0a570, 0x054e4, 0x0d160,
  //2080-2089
  0x0e968, 0x0d520, 0x0daa0, 0x16aa6, 0x056d0, 0x04ae0, 0x0a9d4, 0x0a2d0, 0x0d150, 0x0f252,
  //2090-2099
  0x0d520]; //2100

  var solarMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

  var Gan = ["Jia", "Yi", "Bing", "Ding", "Wu", "Ji", "Geng", "Xin", "Ren", "Gui"];
  var Zhi = ["Zi", "Chou", "Yin", "Mao", "Chen", "Si", "Wu", "Wei", "Shen", "You", "Xu", "Hai"];

  var ChineseZodiac = ["Rat", "Ox", "Tiger", "Rabbit", "Dragon", "Snake", "Horse", "Goat", "Monkey", "Rooster", "Dog", "Pig"];

  var festival = {
    '1-1': {
      title: 'New Year\'s Day'
    },
    '2-14': {
      title: 'Valentine\'s Day'
    },
    '5-1': {
      title: 'Labor Day'
    },
    '5-4': {
      title: 'Youth Day'
    },
    '6-1': {
      title: 'Children\'s Day'
    },
    '9-10': {
      title: 'Teachers\' Day'
    },
    '10-1': {
      title: 'National Day'
    },
    '12-25': {
      title: 'Christmas'
    },
    '3-8': {
      title: 'Women\'s Day'
    },
    '3-12': {
      title: 'Tree Planting Day'
    },
    '4-1': {
      title: 'April Fool\'s Day'
    },
    '5-12': {
      title: 'Nurses\' Day'
    },
    '7-1': {
      title: 'Party Founding Day'
    },
    '8-1': {
      title: 'Army Day'
    },
    '12-24': {
      title: 'Christmas Eve'
    }
  };
  var lFestival = {
    '12-30': {
      title: 'New Year\'s Eve'
    },
    '1-1': {
      title: 'Spring Festival'
    },
    '1-15': {
      title: 'Lantern Festival'
    },
    '2-2': {
      title: 'Dragon Raising Head'
    },
    '5-5': {
      title: 'Duanwu Festival'
    },
    '7-7': {
      title: 'Qixi Festival'
    },
    '7-15': {
      title: 'Ghost Festival'
    },
    '8-15': {
      title: 'Mid-Autumn Festival'
    },
    '9-9': {
      title: 'Chongyang Festival'
    },
    '10-1': {
      title: 'Cold Clothes Festival'
    },
    '10-15': {
      title: 'Xiaoyuan Festival'
    },
    '12-8': {
      title: 'Laba Festival'
    },
    '12-23': {
      title: 'Northern Small New Year'
    },
    '12-24': {
      title: 'Southern Small New Year'
    }
  };

  var solarTerm = ["Minor Cold", "Major Cold", "Start of Spring", "Rain Water", "Awakening of Insects", "Spring Equinox", "Clear and Bright", "Grain Rain", "Start of Summer", "Minor Full", "Grain in Ear", "Summer Solstice", "Minor Heat", "Major Heat", "Start of Autumn", "End of Heat", "White Dew", "Autumn Equinox", "Cold Dew", "Frost's Descent", "Start of Winter", "Minor Snow", "Major Snow", "Winter Solstice"];
  var sTermInfo = ['9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c3598082c95f8c965cc920f', '97bd0b06bdb0722c965ce1cfcc920f', 'b027097bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c359801ec95f8c965cc920f', '97bd0b06bdb0722c965ce1cfcc920f', 'b027097bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c359801ec95f8c965cc920f', '97bd0b06bdb0722c965ce1cfcc920f', 'b027097bd097c36b0b6fc9274c91aa', '9778397bd19801ec9210c965cc920e', '97b6b97bd19801ec95f8c965cc920f', '97bd09801d98082c95f8e1cfcc920f', '97bd097bd097c36b0b6fc9210c8dc2', '9778397bd197c36c9210c9274c91aa', '97b6b97bd19801ec95f8c965cc920e', '97bd09801d98082c95f8e1cfcc920f', '97bd097bd097c36b0b6fc9210c8dc2', '9778397bd097c36c9210c9274c91aa', '97b6b97bd19801ec95f8c965cc920e', '97bcf97c3598082c95f8e1cfcc920f', '97bd097bd097c36b0b6fc9210c8dc2', '9778397bd097c36c9210c9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c3598082c95f8c965cc920f', '97bd097bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c3598082c95f8c965cc920f', '97bd097bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c359801ec95f8c965cc920f', '97bd097bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c359801ec95f8c965cc920f', '97bd097bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf97c359801ec95f8c965cc920f', '97bd097bd07f595b0b6fc920fb0722', '9778397bd097c36b0b6fc9210c8dc2', '9778397bd19801ec9210c9274c920e', '97b6b97bd19801ec95f8c965cc920f', '97bd07f5307f595b0b0bc920fb0722', '7f0e397bd097c36b0b6fc9210c8dc2', '9778397bd097c36c9210c9274c920e', '97b6b97bd19801ec95f8c965cc920f', '97bd07f5307f595b0b0bc920fb0722', '7f0e397bd097c36b0b6fc9210c8dc2', '9778397bd097c36c9210c9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bd07f1487f595b0b0bc920fb0722', '7f0e397bd097c36b0b6fc9210c8dc2', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf7f1487f595b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf7f1487f595b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf7f1487f531b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c965cc920e', '97bcf7f1487f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b97bd19801ec9210c9274c920e', '97bcf7f0e47f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '9778397bd097c36b0b6fc9210c91aa', '97b6b97bd197c36c9210c9274c920e', '97bcf7f0e47f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '9778397bd097c36b0b6fc9210c8dc2', '9778397bd097c36c9210c9274c920e', '97b6b7f0e47f531b0723b0b6fb0722', '7f0e37f5307f595b0b0bc920fb0722', '7f0e397bd097c35b0b6fc9210c8dc2', '9778397bd097c36b0b70c9274c91aa', '97b6b7f0e47f531b0723b0b6fb0721', '7f0e37f1487f595b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc9210c8dc2', '9778397bd097c36b0b6fc9274c91aa', '97b6b7f0e47f531b0723b0b6fb0721', '7f0e27f1487f595b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '9778397bd097c36b0b6fc9274c91aa', '97b6b7f0e47f531b0723b0787b0721', '7f0e27f0e47f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '9778397bd097c36b0b6fc9210c91aa', '97b6b7f0e47f149b0723b0787b0721', '7f0e27f0e47f531b0723b0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '9778397bd097c36b0b6fc9210c8dc2', '977837f0e37f149b0723b0787b0721', '7f07e7f0e47f531b0723b0b6fb0722', '7f0e37f5307f595b0b0bc920fb0722', '7f0e397bd097c35b0b6fc9210c8dc2', '977837f0e37f14998082b0787b0721', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e37f1487f595b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc9210c8dc2', '977837f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '977837f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd097c35b0b6fc920fb0722', '977837f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '977837f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '977837f0e37f14998082b0787b06bd', '7f07e7f0e47f149b0723b0787b0721', '7f0e27f0e47f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '977837f0e37f14998082b0723b06bd', '7f07e7f0e37f149b0723b0787b0721', '7f0e27f0e47f531b0b0bb0b6fb0722', '7f0e397bd07f595b0b0bc920fb0722', '977837f0e37f14898082b0723b02d5', '7ec967f0e37f14998082b0787b0721', '7f07e7f0e47f531b0723b0b6fb0722', '7f0e37f1487f595b0b0bb0b6fb0722', '7f0e37f0e37f14898082b0723b02d5', '7ec967f0e37f14998082b0787b0721', '7f07e7f0e47f531b0723b0b6fb0722', '7f0e37f1487f531b0b0bb0b6fb0722', '7f0e37f0e37f14898082b0723b02d5', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e37f1487f531b0b0bb0b6fb0722', '7f0e37f0e37f14898082b072297c35', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e37f0e37f14898082b072297c35', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e37f0e366aa89801eb072297c35', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f149b0723b0787b0721', '7f0e27f1487f531b0b0bb0b6fb0722', '7f0e37f0e366aa89801eb072297c35', '7ec967f0e37f14998082b0723b06bd', '7f07e7f0e47f149b0723b0787b0721', '7f0e27f0e47f531b0b0bb0b6fb0722', '7f0e37f0e366aa89801eb072297c35', '7ec967f0e37f14998082b0723b06bd', '7f07e7f0e37f14998083b0787b0721', '7f0e27f0e47f531b0b0bb0b6fb0722', '7f0e37f0e366aa89801eb072297c35', '7ec967f0e37f14898082b0723b02d5', '7f07e7f0e37f14998082b0787b0721', '7f07e7f0e47f531b0b0bb0b6fb0722', '7f0e36665b66aa89801e9808297c35', '665f67f0e37f14898082b0723b02d5', '7ec967f0e37f14998082b0787b0721', '7f07e7f0e47f531b0723b0b6fb0722', '7f0e36665b66a449801e9808297c35', '665f67f0e37f14898082b0723b02d5', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e36665b66a449801e9808297c35', '665f67f0e37f14898082b072297c35', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e26665b66a449801e9808297c35', '665f67f0e37f1489801eb072297c35', '7ec967f0e37f14998082b0787b06bd', '7f07e7f0e47f531b0723b0b6fb0721', '7f0e27f1487f531b0b0bb0b6fb0722'];

  var nStr1 = ["Day", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
  var nStr2 = ["First", "Tenth", "Twentieth", "Thirtieth"];
  var nStr3 = ["Positive", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Winter", "Laparoscopic"];

  /**
   * @1900-2100 range of Gregorian and lunar conversion
   * @charset UTF-8
   * @Author  Jea Yang (JJonline@JJonline.Cn)
   * @Time    2014-7-21
   * @Time    2016-8-13 Fixed 2033hex, Attribution Annals
   * @Time    2016-9-25 Fixed lunar LeapMonth Param Bug
   * @Time    2017-7-24 Fixed use getTerm Func Param Error. use solar year, NOT lunar year
   * @Version 1.0.3
   * @Convert Gregorian to Lunar: calendar.solar2lunar(1987,11,01); //[you can ignore params of prefix 0]
   * @Convert Lunar to Gregorian: calendar.lunar2solar(1987,09,10); //[you can ignore params of prefix 0]
   */
  var calendar = {
    /**
     * Lunar 1900-2100 leap size information table
     * @Array Of Property
     * @return Hex
     */
    lunarInfo: lunarInfo,
    /**
     * Gregorian month days normal table
     * @Array Of Property
     * @return Number
     */
    solarMonth: solarMonth,
    /**
     * Heavenly stems and earthly branches quick lookup table for heavenly stems
     * @Array Of Property trans["Jia","Yi","Bing","Ding","Wu","Ji","Geng","Xin","Ren","Gui"]
     * @return English string
     */
    Gan: Gan,
    /**
     * Heavenly stems and earthly branches quick lookup table for earthly branches
     * @Array Of Property
     * @trans["Zi","Chou","Yin","Mao","Chen","Si","Wu","Wei","Shen","You","Xu","Hai"]
     * @return English string
     */
    Zhi: Zhi,
    /**
     * Heavenly stems and earthly branches quick lookup table <=> Zodiac
     * @Array Of Property
     * @trans["Rat","Ox","Tiger","Rabbit","Dragon","Snake","Horse","Goat","Monkey","Rooster","Dog","Pig"]
     * @return English string
     */
    Animals: ChineseZodiac,
    /**
     * Solar calendar festivals
     */
    festival: festival,
    /**
     * Lunar calendar festivals
     */
    lFestival: lFestival,
    /**
     * 24 solar terms quick lookup table
     * @Array Of Property
     * @trans["Minor Cold","Major Cold","Start of Spring","Rain Water","Awakening of Insects","Spring Equinox","Clear and Bright","Grain Rain","Start of Summer","Minor Full","Grain in Ear","Summer Solstice","Minor Heat","Major Heat","Start of Autumn","End of Heat","White Dew","Autumn Equinox","Cold Dew","Frost's Descent","Start of Winter","Minor Snow","Major Snow","Winter Solstice"]
     * @return English string
     */
    solarTerm: solarTerm,
    /**
     * 1900-2100 each year's 24 solar term date quick lookup table
     * @Array Of Property
     * @return 0x string For splice
     */
    sTermInfo: sTermInfo,
    /**
     * Number to Chinese quick lookup table
     * @Array Of Property
     * @trans ['Day','One','Two','Three','Four','Five','Six','Seven','Eight','Nine','Ten']
     * @return English string
     */
    nStr1: nStr1,
    /**
     * Date to lunar title quick lookup table
     * @Array Of Property
     * @trans ['First','Tenth','Twentieth','Thirtieth']
     * @return English string
     */
    nStr2: nStr2,
    /**
     * Month to lunar title quick lookup table
     * @Array Of Property
     * @trans ['Positive','One','Two','Three','Four','Five','Six','Seven','Eight','Nine','Ten','Winter','Laparoscopic']
     * @return English string
     */
    nStr3: nStr3,
    /**
     * Returns the default defined solar festivals
     */
    getFestival: function getFestival() {
      return this.festival;
    },
    /**
     * Returns the default defined lunar festivals
     */
    getLunarFestival: function getLunarFestival() {
      return this.lFestival;
    },
    /**
     *
     * @param param {Object} Input data according to the festival format, set solar festivals
     */
    setFestival: function setFestival() {
      var param = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      this.festival = param;
    },
    /**
     *
     * @param param {Object} Input data according to the lFestival format, set lunar festivals
     */
    setLunarFestival: function setLunarFestival() {
      var param = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
      this.lFestival = param;
    },
    /**
     * Returns the total number of days in the lunar year y
     * @param y lunar Year
     * @return Number
     * @eg:var count = calendar.lYearDays(1987) ;//count=387
     */
    lYearDays: function lYearDays(y) {
      var i,
        sum = 348;
      for (i = 0x8000; i > 0x8; i >>= 1) {
        sum += this.lunarInfo[y - 1900] & i ? 1 : 0;
      }
      return sum + this.leapDays(y);
    },
    /**
     * Returns which month is the leap month in lunar year y; if there is no leap month in year y, return 0
     * @param y lunar Year
     * @return Number (0-12)
     * @eg:var leapMonth = calendar.leapMonth(1987) ;//leapMonth=6
     */
    leapMonth: function leapMonth(y) {
      // Leap character encoding
      return this.lunarInfo[y - 1900] & 0xf;
    },
    /**
     * Returns the number of days in the leap month of lunar year y; if there is no leap month in that year, return 0
     * @param y lunar Year
     * @return Number (0, 29, 30)
     * @eg:var leapMonthDay = calendar.leapDays(1987) ;//leapMonthDay=29
     */
    leapDays: function leapDays(y) {
      if (this.leapMonth(y)) {
        return this.lunarInfo[y - 1900] & 0x10000 ? 30 : 29;
      }
      return 0;
    },
    /**
     * Returns the total number of days in lunar year y month m (non-leap month); to calculate the number of days in month m as a leap month, please use the leapDays method
     * @param y lunar Year
     * @param m lunar Month
     * @return Number (-1, 29, 30)
     * @eg:var MonthDay = calendar.monthDays(1987,9) ;//MonthDay=29
     */
    monthDays: function monthDays(y, m) {
      if (m > 12 || m < 1) {
        return -1;
      } // Month parameter ranges from 1 to 12, return -1 for parameter error
      return this.lunarInfo[y - 1900] & 0x10000 >> m ? 30 : 29;
    },
    /**
     * Returns the number of days in the Gregorian year y month m
     * @param y solar Year
     * @param m solar Month
     * @return Number (-1, 28, 29, 30, 31)
     * @eg:var solarMonthDay = calendar.leapDays(1987) ;//solarMonthDay=30
     */
    solarDays: function solarDays(y, m) {
      if (m > 12 || m < 1) {
        return -1;
      } // Return -1 for parameter error
      var ms = m - 1;
      if (ms === 1) {
        // The leap and flat rules for February confirm to return 28 or 29
        return y % 4 === 0 && y % 100 !== 0 || y % 400 === 0 ? 29 : 28;
      } else {
        return this.solarMonth[ms];
      }
    },
    /**
     * Converts lunar year to heavenly stems and earthly branches
     * @param  lYear Lunar year number
     * @return English string
     */
    toGanZhiYear: function toGanZhiYear(lYear) {
      var ganKey = (lYear - 3) % 10;
      var zhiKey = (lYear - 3) % 12;
      if (ganKey === 0) ganKey = 10; // If the remainder is 0, it is the last heavenly stem
      if (zhiKey === 0) zhiKey = 12; // If the remainder is 0, it is the last earthly branch
      return this.Gan[ganKey - 1] + this.Zhi[zhiKey - 1];
    },
    /**
     * Determines the zodiac sign based on the Gregorian month and day
     * @param  cMonth [description]
     * @param  cDay [description]
     * @return English string
     */
    toAstro: function toAstro(cMonth, cDay) {
      var s = "Capricorn Aquarius Pisces Aries Taurus Gemini Cancer Leo Virgo Libra Scorpio Sagittarius Capricorn";
      var arr = [20, 19, 21, 21, 21, 22, 23, 23, 23, 23, 22, 22];
      return s.substr(cMonth * 2 - (cDay < arr[cMonth - 1] ? 2 : 0), 2) + " sign"; // sign
    },

    /**
     * Returns heavenly stems and earthly branches based on the offset
     * @param offset Offset relative to the Jiazi
     * @return English string
     */
    toGanZhi: function toGanZhi(offset) {
      return this.Gan[offset % 10] + this.Zhi[offset % 12];
    },
    /**
     * Returns the Gregorian date of the nth solar term in year y
     * @param y Gregorian year (1900-2100)
     * @param n The nth solar term (1~24); starting from n=1 (Minor Cold)
     * @return day Number
     * @eg:var _24 = calendar.getTerm(1987,3) ;//_24=4; meaning February 4, 1987 is the Start of Spring
     */
    getTerm: function getTerm(y, n) {
      if (y < 1900 || y > 2100 || n < 1 || n > 24) {
        return -1;
      }
      var _table = this.sTermInfo[y - 1900];
      var _calcDay = [];
      for (var index = 0; index < _table.length; index += 5) {
        var chunk = parseInt('0x' + _table.substr(index, 5)).toString();
        _calcDay.push(chunk[0], chunk.substr(1, 2), chunk[3], chunk.substr(4, 2));
      }
      return parseInt(_calcDay[n - 1]);
    },
    /**
     * Returns the colloquial expression of the lunar month based on the lunar month number
     * @param m lunar month
     * @return English string
     * @eg:var cnMonth = calendar.toChinaMonth(12) ;//cnMonth='Laparoscopic Month'
     */
    toChinaMonth: function toChinaMonth(m) {
      // Month => 月
      if (m > 12 || m < 1) {
        return -1;
      } // Return -1 for parameter error
      var s = this.nStr3[m - 1];
      s += " Month"; // Add the word "Month"
      return s;
    },
    /**
     * Returns the Chinese character representation of the lunar date number
     * @param d lunar day
     * @return English string
     * @eg:var cnDay = calendar.toChinaDay(21) ;//cnMonth='Twenty One'
     */
    toChinaDay: function toChinaDay(d) {
      // Day => 日
      var s;
      switch (d) {
        case 10:
          s = "First Ten";
          break;
        case 20:
          s = "Second Ten";
          break;
        case 30:
          s = "Third Ten";
          break;
        default:
          s = this.nStr2[Math.floor(d / 10)];
          s += this.nStr1[d % 10];
      }
      return s;
    },
    /**
     * Converts year to zodiac sign [!only approximate conversion] => The precise division of zodiac sign boundary is "Start of Spring"
     * @param y year
     * @return English string
     * @eg:var animal = calendar.getAnimal(1987) ;//animal='Rabbit'
     */
    getAnimal: function getAnimal(y) {
      return this.Animals[(y - 4) % 12];
    },
    /**
     * Returns detailed Gregorian and lunar object information based on the input Gregorian year, month, and day <=> JSON
     * !important! Gregorian parameter range 1900.1.31~2100.12.31
     * @param yPara  solar year
     * @param mPara  solar month
     * @param dPara  solar day
     * @return JSON object
     * @eg:console.log(calendar.solar2lunar(1987,11,01));
     */
    solar2lunar: function solar2lunar(yPara, mPara, dPara) {
      var y = parseInt(yPara);
      var m = parseInt(mPara);
      var d = parseInt(dPara);
      // Year limit, upper limit
      if (y < 1900 || y > 2100) {
        return -1; // undefined converted to number becomes NaN
      }
      // Minimum limit for Gregorian parameters
      if (y === 1900 && m === 1 && d < 31) {
        return -1;
      }

      // If no parameters are passed, get today's date
      var objDate;
      if (!y) {
        objDate = new Date();
      } else {
        objDate = new Date(y, parseInt(m) - 1, d);
      }
      var i,
        leap = 0,
        temp = 0;
      // Correct ymd parameters
      y = objDate.getFullYear();
      m = objDate.getMonth() + 1;
      d = objDate.getDate();
      var offset = (Date.UTC(objDate.getFullYear(), objDate.getMonth(), objDate.getDate()) - Date.UTC(1900, 0, 31)) / 86400000;
      for (i = 1900; i < 2101 && offset > 0; i++) {
        temp = this.lYearDays(i);
        offset -= temp;
      }
      if (offset < 0) {
        offset += temp;
        i--;
      }

      // Is it today
      var isTodayObj = new Date(),
        isToday = false;
      if (isTodayObj.getFullYear() === y && isTodayObj.getMonth() + 1 === m && isTodayObj.getDate() === d) {
        isToday = true;
      }
      // What day of the week
      var nWeek = objDate.getDay(),
        cWeek = this.nStr1[nWeek];
      // The numerical representation of the day of the week follows the convention of starting the week on Monday
      if (nWeek === 0) {
        nWeek = 7;
      }
      // Lunar year
      var year = i;
      leap = this.leapMonth(i); // Which month is the leap month
      var isLeap = false;

      // Validate leap month
      for (i = 1; i < 13 && offset > 0; i++) {
        // Leap month
        if (leap > 0 && i === leap + 1 && isLeap === false) {
          --i;
          isLeap = true;
          temp = this.leapDays(year); // Calculate the number of days in the lunar leap month
        } else {
          temp = this.monthDays(year, i); // Calculate the number of days in the lunar normal month
        }
        // Release leap month
        if (isLeap === true && i === leap + 1) {
          isLeap = false;
        }
        offset -= temp;
      }
      // Leap month causes array index overlap
      if (offset === 0 && leap > 0 && i === leap + 1) {
        if (isLeap) {
          isLeap = false;
        } else {
          isLeap = true;
          --i;
        }
      }
      if (offset < 0) {
        offset += temp;
        --i;
      }
      // Lunar month
      var month = i;
      // Lunar day
      var day = offset + 1;
      // Heavenly stems and earthly branches processing
      var sm = m - 1;
      var gzY = this.toGanZhiYear(year);

      // The two solar terms of the month
      // bugfix-2017-7-24 11:03:38 use lunar Year Param `y` Not `year`
      var firstNode = this.getTerm(y, m * 2 - 1); // Returns the start date of the solar term for the month
      var secondNode = this.getTerm(y, m * 2); // Returns the start date of the solar term for the month

      // Adjust heavenly stems and earthly branches month based on 12 solar terms
      var gzM = this.toGanZhi((y - 1900) * 12 + m + 11);
      if (d >= firstNode) {
        gzM = this.toGanZhi((y - 1900) * 12 + m + 12);
      }

      // Whether the input date is a solar term
      var isTerm = false;
      var Term = null;
      if (firstNode === d) {
        isTerm = true;
        Term = this.solarTerm[m * 2 - 2];
      }
      if (secondNode === d) {
        isTerm = true;
        Term = this.solarTerm[m * 2 - 1];
      }
      // Day pillar The difference in days between the first day of the month and 1900/1/1
      var dayCyclical = Date.UTC(y, sm, 1, 0, 0, 0, 0) / 86400000 + 25567 + 10;
      var gzD = this.toGanZhi(dayCyclical + d - 1);
      // The zodiac sign of the date
      var astro = this.toAstro(m, d);
      var solarDate = y + '-' + m + '-' + d;
      var lunarDate = year + '-' + month + '-' + day;
      var festival = this.festival;
      var lFestival = this.lFestival;
      var festivalDate = m + '-' + d;
      var lunarFestivalDate = month + '-' + day;

      // bugfix https://github.com/jjonline/calendar.js/issues/29
      // Lunar festival correction: If the lunar December is a small month, then the 29th is New Year's Eve, if it is a big month, then the 30th is New Year's Eve
      // Here is a clever fix: when it is the 29th of lunar December, add a judgment and set lunarFestivalDate to 12-30 to correctly obtain New Year's Eve
      // The principle of lunar festivals in the Celestial Empire is to pass before and not after, here the number of days in lunar December is taken without considering the leap month
      // The lunar leap December only appears in the 200-year range supported by this tool in 1574
      if (month === 12 && day === 29 && this.monthDays(year, month) === 29) {
        lunarFestivalDate = '12-30';
      }
      return {
        date: solarDate,
        lunarDate: lunarDate,
        festival: festival[festivalDate] ? festival[festivalDate].title : null,
        lunarFestival: lFestival[lunarFestivalDate] ? lFestival[lunarFestivalDate].title : null,
        'lYear': year,
        'lMonth': month,
        'lDay': day,
        'Animal': this.getAnimal(year),
        'IMonthCn': (isLeap ? "Leap" : '') + this.toChinaMonth(month),
        'IDayCn': this.toChinaDay(day),
        'cYear': y,
        'cMonth': m,
        'cDay': d,
        'gzYear': gzY,
        'gzMonth': gzM,
        'gzDay': gzD,
        'isToday': isToday,
        'isLeap': isLeap,
        'nWeek': nWeek,
        'ncWeek': "Week " + cWeek,
        'isTerm': isTerm,
        'Term': Term,
        'astro': astro
      };
    },
    /**
     * Pass in lunar year, month, day and whether the month is a leap month to obtain detailed Gregorian and lunar object information <=> JSON
     * !important! Parameter range 1900.1.31~2100.12.1
     * @param y  lunar year
     * @param m  lunar month
     * @param d  lunar day
     * @param isLeapMonth  lunar month is leap or not. [If it is a lunar leap month, assign true to the fourth parameter]
     * @return JSON object
     * @eg:console.log(calendar.lunar2solar(1987,9,10));
     */
    lunar2solar: function lunar2solar(y, m, d, isLeapMonth) {
      y = parseInt(y);
      m = parseInt(m);
      d = parseInt(d);
      isLeapMonth = !!isLeapMonth;
      var leapMonth = this.leapMonth(y);
      this.leapDays(y);
      if (isLeapMonth && leapMonth !== m) {
        return -1;
      } // The parameter requires calculating the Gregorian calendar for the leap month, but the leap month obtained in that year is different from the month passed in
      if (y === 2100 && m === 12 && d > 1 || y === 1900 && m === 1 && d < 31) {
        return -1;
      } // Exceeds the maximum limit
      var day = this.monthDays(y, m);
      var _day = day;
      // bugFix 2016-9-25
      // if month is leap, _day use leapDays method
      if (isLeapMonth) {
        _day = this.leapDays(y, m);
      }
      if (y < 1900 || y > 2100 || d > _day) {
        return -1;
      } // Parameter legality verification

      // Calculate the time difference in the lunar calendar
      var offset = 0;
      var i;
      for (i = 1900; i < y; i++) {
        offset += this.lYearDays(i);
      }
      var leap = 0,
        isAdd = false;
      for (i = 1; i < m; i++) {
        leap = this.leapMonth(y);
        if (!isAdd) {
          // Handle leap month
          if (leap <= i && leap > 0) {
            offset += this.leapDays(y);
            isAdd = true;
          }
        }
        offset += this.monthDays(y, i);
      }
      // Convert lunar leap month, need to supplement the time difference of the previous month of the leap month in that year
      if (isLeapMonth) {
        offset += day;
      }
      // The Gregorian time of the first day of the lunar calendar in 1900 is January 30, 1900, 0:00:00 (this time is also the starting point of this lunar calendar)
      var strap = Date.UTC(1900, 1, 30, 0, 0, 0);
      var calObj = new Date((offset + d - 31) * 86400000 + strap);
      var cY = calObj.getUTCFullYear();
      var cM = calObj.getUTCMonth() + 1;
      var cD = calObj.getUTCDate();
      return this.solar2lunar(cY, cM, cD);
    }
  };

  return calendar;

})();

export default calendar;