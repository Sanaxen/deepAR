
def forecast_plot(plot_step, range_start, range_stop, plot_start, dim, predictor, data, outpu_image_file):
    forecast_it, ts_it = make_evaluation_predictions(
        dataset= data,  # test dataset
        predictor= predictor,  # predictor
        num_samples= 100,  # number of sample paths we want for evaluation
    )
    
    #plot_length = data_length-(seq_length+predict_length)
    prediction_intervals = (50.0, 90.0)
    legend = ["observations", "median prediction"] + [f"{k}% prediction interval" for k in prediction_intervals] [::-1]
    
    m = min(plot_step, range_stop-range_start)
    plt.figure(figsize=(6.4*m, 4.8*m))
    for x, y in zip(ts_it, forecast_it) :
        for i in range(range_start, range_stop) :
            plt.subplot(m, 1, i-range_start + 1)
            plt.title(titles[i])
            x[i][-(plot_start):].plot()
            y.copy_dim(i).plot(color= 'g', prediction_intervals= prediction_intervals)
            plt.grid(which='both')
            plt.legend(legend, loc='upper left')
            plt.axvline(df.index[data_length-(seq_length+predict_length)], color='b') # end of train dataset
            plt.axvline(df.index[data_length-(predict_length)], color='r') # end of train dataset

    plt.savefig(outpu_image_file)
    #plt.show()

